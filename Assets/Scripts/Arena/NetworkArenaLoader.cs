using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
public class NetworkArenaLoader : NetworkBehaviour
{

    [SyncVar(hook=nameof(HookArenaId))]
    private string arenaId;

    [Space]

    [SerializeField] private NetworkArena arena;
    [SerializeField] private NetworkMatchScoreUI scoreUI;

    [Space]

    [SerializeField] private ArenaResources arenaResources;

    [Space]

    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallsTilemap;

    private readonly List<UnityAction> _serverArenaStartListeners = new();
    private readonly List<UnityAction> _serverArenaStopListeners = new();
    private readonly List<GameObject> _serverSpawnedIdentities = new();
    private readonly List<GameObject> _spawnedObjects = new();

    public override void OnStopServer()
    {
        Cleanup();
    }

    private void OnEnable()
    {
        if (isClient)
            LoadArena(arenaId);
    }

    private void HookArenaId(string old, string current)
    {
        if (!isServer)
            LoadArena(arenaId);
    }

    private Vector2 LoadTileMap(string tilemapData)
    {
        var lines = tilemapData
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        int width = lines[0].Length;
        int height = lines.Length;

        var size = new Vector3Int(width, height, 0);
        floorTilemap.size = size;
        wallsTilemap.size = size;

        var offset = new Vector2(width / -2f, height / -2f);
        floorTilemap.transform.localPosition = offset;
        wallsTilemap.transform.localPosition = offset;

        var selfPos = Vector3Int.RoundToInt(transform.position);
        for (int y = 0; y < height; y++)
        {
            var line = lines[y];

            for (int x = 0; x < width; x++)
            {
                if (!char.IsDigit(line[x]))
                    continue;

                int tile = line[x] - '0';

                if (tile >= arenaResources.tiles.Length)
                    continue;

                var tileData = arenaResources.tiles[tile];

                int variantIndex = ((x * 73856093) ^ (y * 19349663)) % tileData.tileVariants.Length;
                if (variantIndex < 0)
                    variantIndex = -variantIndex;

                int invertedY = height - 1 - y;

                if (tileData.type == ArenaResources.TileData.Type.Floor)
                {
                    floorTilemap.SetTile(
                        new Vector3Int(x, invertedY, 0),
                        tileData.tileVariants[variantIndex]);
                }
                else if (tileData.type == ArenaResources.TileData.Type.Wall)
                {
                    wallsTilemap.SetTile(
                        new Vector3Int(x, invertedY, 0),
                        tileData.tileVariants[variantIndex]);
                }
            }
        }

        return offset;
    }

    private void LoadPlayerGroupSpawnPoses(ArenaData.PlayerGroup[] groups, Vector2 offset)
    {
        arena.serverSpawnGroups.Clear();
        foreach (var groupData in groups)
        {
            var team = GameTanksManager.Singleton.GetTeam(groupData.team);
            if (team == default)
                team = GameTanksManager.Singleton.teams[0];
            var group = new NetworkArena.ServerSpawnGroup()
            {
                teamId = team.id,
                respawnDelay = groupData.respawnDelay,
                spawnPoints = groupData.spawnPoints.Select(spawnPoint =>
                {
                    var obj = Instantiate(
                        arenaResources.playerSpawn,
                        (Vector2)transform.position + (Vector2)spawnPoint + offset,
                        Quaternion.Euler(0, 0, spawnPoint.z),
                        parent: transform);
                    if (obj.TryGetComponent(out SpriteRenderer spr))
                        spr.color = new Color(team.color.r, team.color.g, team.color.b, spr.color.a);
                    _spawnedObjects.Add(obj);
                    return obj.transform;
                }).ToArray()
            };
            if (isServer)
            {
                arena.serverSpawnGroups.Add(group);
            }
        }
    }

    [Server]
    private void ServerLoadItemSpawners(ArenaData.ItemSpawner[] itemSpawners, Vector2 offset)
    {
        foreach (var item in itemSpawners)
        {
            var prefab = item.type switch
            {
                ArenaData.ItemSpawner.Type.boost => arenaResources.boostSpawner,
                ArenaData.ItemSpawner.Type.gear => arenaResources.gearSpawner,
                ArenaData.ItemSpawner.Type.shield => arenaResources.shieldSpawner,
                _ => null
            };
            if (!prefab)
                continue;
            var itemSpawnerObject = Instantiate(prefab, (Vector2)transform.position + item.pos + offset, Quaternion.identity);
            if (!itemSpawnerObject.TryGetComponent(out NetworkSingleStructureSpawner spawnerComponent))
            {
                Destroy(itemSpawnerObject);
                continue;
            }
            NetworkServer.Spawn(itemSpawnerObject);
            spawnerComponent.ServerResetAndSetEnabled(false);
            spawnerComponent.respawnDelay = item.delay;
            void startListener() => spawnerComponent.ServerResetAndSetEnabled(true);
            void stopListener() => spawnerComponent.ServerResetAndSetEnabled(false);
            _serverArenaStartListeners.Add(startListener);
            _serverArenaStopListeners.Add(stopListener);
            arena.serverOnStartGame.AddListener(startListener);
            arena.serverOnStopGame.AddListener(stopListener);
            _serverSpawnedIdentities.Add(itemSpawnerObject);
        }
    }

    [Server]
    private void ServerSpawnFlags(ArenaData.Flag[] flags, Vector2 offset)
    {
        var spawnedFlags = new List<NetworkCaptureFlag>();
        var index = 0;

        foreach (var flagData in flags)
        {
            var spawnPos = (Vector2)transform.position + flagData.pos + offset;

            var flagObj = Instantiate(arenaResources.flagPrefab, spawnPos, Quaternion.identity);

            if (!flagObj.TryGetComponent(out NetworkCaptureFlag flag))
            {
                Destroy(flagObj);
                continue;
            }
            index++;

            flag.serverStartPrivot = spawnPos;

            NetworkServer.Spawn(flagObj);
            flag.SetTeam(flagData.team);

            _serverSpawnedIdentities.Add(flagObj);
            spawnedFlags.Add(flag);

            if (isServer && index == 1)
            {
                flag.serverOnScore.AddListener((score) => scoreUI.ServerSetFirstTeamScore(score));
                scoreUI.ServerSetFirstTeam(flagData.team);
            }

            if (isServer && index == 2)
            {
                flag.serverOnScore.AddListener((score) => scoreUI.ServerSetSecondTeamScore(score));
                scoreUI.ServerSetSecondTeam(flagData.team);
            }

            if (isServer && index == 3)
            {
                flag.serverOnScore.AddListener((score) => scoreUI.ServerSetThirdTeamScore(score));
                scoreUI.ServerSetThirdTeam(flagData.team);
            }

            if (isServer)
            {
                void startListener() => flag.ServerReset();
                void stopListener() => flag.ServerReset();

                _serverArenaStartListeners.Add(startListener);
                _serverArenaStopListeners.Add(stopListener);

                arena.serverOnStartGame.AddListener(startListener);
                arena.serverOnStopGame.AddListener(stopListener);
            }
        }

        foreach (var flag in spawnedFlags)
            flag.serverOtherFlags.AddRange(spawnedFlags.Where(f => f != flag));
    }

    private void LoadArena(string arenaId)
    {
        if (arenaId == "")
        {
            Cleanup();
            return;
        }
        if (!NetworkArenaLobbyManager.Singleton)
            return;
        ArenaData arenaData = null;
        foreach(var arena in NetworkArenaLobbyManager.Singleton.arenas)
        {
            if (arena.id == arenaId)
            {
                arenaData = arena;
                break;
            }
        }
        if (arenaData == null)
            return;
        if (isServer)
            this.arenaId = arenaId;
        Cleanup();
        var offset = LoadTileMap(arenaData.tilemap);
        LoadPlayerGroupSpawnPoses(arenaData.playerGroups, offset);
        if (isServer)
        {
            ServerLoadItemSpawners(arenaData.itemSpawners, offset);
            ServerSpawnFlags(arenaData.flags, offset);
        }
    }

    [Server]
    public void ServerLoadArena(string arenaId)
    {
        LoadArena(arenaId);
    }

    public void Cleanup(bool isDestroy = false)
    {
        foreach (var spawned in _spawnedObjects)
            if (spawned)
                Destroy(spawned);
        if (!isDestroy && isServer && NetworkServer.active)
            foreach (var spawned in _serverSpawnedIdentities)
                if (spawned)
                    NetworkServer.Destroy(spawned);
        floorTilemap.ClearAllTiles();
        wallsTilemap.ClearAllTiles();
        _spawnedObjects.Clear();
        _serverSpawnedIdentities.Clear();
        foreach (var listener in _serverArenaStartListeners)
            arena.serverOnStartGame.RemoveListener(listener);
        foreach (var listener in _serverArenaStopListeners)
            arena.serverOnStopGame.RemoveListener(listener);
        _serverArenaStartListeners.Clear();
        _serverArenaStopListeners.Clear();
        arena.serverSpawnGroups.Clear();
    }

    [Serializable]
    public class ArenaResources
    {
        public TileData[] tiles;
        public GameObject boostSpawner;
        public GameObject shieldSpawner;
        public GameObject gearSpawner;
        public GameObject playerSpawn;
        public GameObject flagPrefab;

        [Serializable]
        public struct TileData
        {
            public Type type;
            public Tile[] tileVariants;
            public enum Type { Floor, Wall }
        }
    }
}