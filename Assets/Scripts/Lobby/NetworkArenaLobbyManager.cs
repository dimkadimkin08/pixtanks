using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NetworkArenaLobbyManager : NetworkBehaviour
{

    public static NetworkArenaLobbyManager Singleton;

    public ArenaAsset[] preloadArenas;

    public readonly SyncList<ArenaData> arenas = new();

    [SerializeField] private GameObject baseArenaPrefab;
    [SerializeField] private float arenaSpawnDistance = 1000;

    public readonly SyncDictionary<string, ArenaLobby> lobbies = new();

    private readonly Dictionary<string, NetworkArena> _serverInstances = new();

    NetworkArenaLobbyManager()
    {
        Singleton = this;
    }

    private void Awake()
    {
        Singleton = this;
    }

    private void OnEnable()
    {
        GameNetworkManager.Singleton.ServerOnPlayerLeave += ServerOnDisconnected;
    }

    private void OnDisable()
    {
        if (GameNetworkManager.Singleton)
            GameNetworkManager.Singleton.ServerOnPlayerLeave -= ServerOnDisconnected;
    }

    public override void OnStartServer()
    {
        arenas.Clear();
        if (preloadArenas != null && preloadArenas.Length > 0)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Contains("-serverExportBuiltInMaps"))
            {
                ExportBuiltInMaps();
            }
            arenas.AddRange(preloadArenas.Select(arenaAsset => arenaAsset.arenaData));
        }
        LoadMaps();
        //NetworkServer.RegisterHandler<JoinLobbyMessage>(ServerJoinLobby);
        //NetworkServer.RegisterHandler<CreateLobbyMessage>(ServerCreateLobby);
    }

    public override void OnStopServer()
    {
        //NetworkServer.UnregisterHandler<JoinLobbyMessage>();
        //NetworkServer.UnregisterHandler<CreateLobbyMessage>();
    }

    private void ExportBuiltInMaps()
    {
        int i = 0;
        foreach (var preloadMap in preloadArenas)
        {
            i++;
            try
            {
                ArenaData map = preloadMap.arenaData;

                string json = JsonUtility.ToJson(map, true);

                string savePath = Path.Combine(Application.dataPath, $"preload-map-{i}.json");

                File.WriteAllText(savePath, json);

                Debug.Log($"Expored built in map: {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Expored built in map: {e.Message}");
            }
        }
    }

    private void LoadMaps()
    {
        string mapsPath = $"{Directory.GetCurrentDirectory()}/Maps";

        if (!Directory.Exists(mapsPath))
        {
            Debug.Log($"Maps folder not found: {mapsPath}");
            return;
        }

        string[] files = Directory.GetFiles(mapsPath, "*.json", SearchOption.AllDirectories);

        Dictionary<string, ArenaData> loadedMaps = new();

        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                ArenaData map = JsonUtility.FromJson<ArenaData>(json);

                if (map != null && !string.IsNullOrEmpty(map.id))
                {
                    loadedMaps[map.id] = map;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error reading map file {file}: {e.Message}");
            }
        }

        HashSet<string> loadedIds = new(loadedMaps.Keys);

        arenas.RemoveAll(a => a != null && loadedIds.Contains(a.id));

        arenas.AddRange(loadedMaps.Select(map => map.Value));
    }

    private NetworkArena SpawnArena(string lobbyId, string arenaId)
    {
        Vector2 GetPosition(int xIndex, int yIndex)
        {
            return new Vector2(
                xIndex * arenaSpawnDistance,
                yIndex * arenaSpawnDistance
            );
        }

        bool IsOccupied(Vector2 position)
        {
            foreach (var instance in _serverInstances.Values)
            {
                if (instance == null)
                    continue;

                Vector2 instancePos = instance.transform.position;

                if (Vector2.Distance(instancePos, position) < 1f)
                    return true;
            }

            return false;
        }

        int ring = 1;

        while (true)
        {
            for (int x = -ring; x <= ring; x++)
            {
                for (int y = -ring; y <= ring; y++)
                {
                    if (x == 0 && y == 0)
                        continue;
                    if (Mathf.Abs(x) != ring && Mathf.Abs(y) != ring)
                        continue;

                    Vector2 candidate = GetPosition(x, y);

                    if (candidate == Vector2.zero)
                        continue;

                    if (!IsOccupied(candidate))
                    {
                        var obj = Instantiate(baseArenaPrefab, candidate, Quaternion.identity);
                        if (!obj.TryGetComponent(out NetworkArena arena))
                        {
                            Destroy(obj);
                            return null;
                        }
                        NetworkServer.Spawn(obj);
                        arena.ServerInitArena(arenaId);

                        _serverInstances.Add(lobbyId, arena);

                        return arena;
                    }
                }
            }

            ring++;
        }
    }

    [Server]
    public bool ServerCreateLobby(CreateLobbyMessage message)
    {
        var arenaData = arenas.FirstOrDefault(arena => arena.id == message.arenaId);
        if (arenaData == default)
            return false;
        if (lobbies.ContainsKey(message.lobbyId))
            return false;
        var spawnedArena = SpawnArena(message.lobbyId, arenaData.id);
        if (!spawnedArena)
            return false;
        lobbies.Add(message.lobbyId, new()
        {
            lobbyId = message.lobbyId,
            arenaId = message.arenaId,
            players = new ArenaLobby.Player[0],
            groups = spawnedArena.serverSpawnGroups.Select(g =>
            {
                return new ArenaLobby.PlayerGroup()
                {
                    teamId = g.teamId,
                    respawn = g.respawnDelay
                };
            }).ToArray()
        });
        return true;
    }

    [Server]
    public bool ServerStartLobby(string lobbyId)
    {
        if (_serverInstances.TryGetValue(lobbyId, out var spawnedArena))
        {
            spawnedArena.ServerStartGame();
            return true;
        }
        return false;
    }

    [Server]
    public bool ServerRestartLobby(string lobbyId)
    {
        if (_serverInstances.TryGetValue(lobbyId, out var spawnedArena))
        {
            spawnedArena.ServerStopGame();
            spawnedArena.ServerStartGame();
            return true;
        }
        return false;
    }

    [Server]
    public bool ServerResetLobby(string lobbyId)
    {
        if (_serverInstances.TryGetValue(lobbyId, out var spawnedArena))
        {
            spawnedArena.ServerClearAllSlots();
            return true;
        }
        return false;
    }

    [Server]
    public bool ServerStopLobby(string lobbyId)
    {
        if (_serverInstances.TryGetValue(lobbyId, out var spawnedArena))
        {
            spawnedArena.ServerStopGame();
            return true;
        }
        return false;
    }

    [Server]
    public bool ServerDeleteLobby(string lobbyId)
    {
        bool success = false;

        if (_serverInstances.TryGetValue(lobbyId, out var spawnedArena))
        {
            spawnedArena.ServerDestroyArena();
            success = true;
        }

        _serverInstances.Remove(lobbyId);
        lobbies.Remove(lobbyId);

        return success;
    }

    [Server]
    public bool ServerJoinLobby(NetworkConnectionToClient conn, JoinLobbyMessage message)
    {
        if (!lobbies.TryGetValue(message.lobbyId, out var lobby))
            return false;

        if (message.group < 0 || lobby.groups.Length <= message.group)
            return false;

        ServerLeaveLobby(conn);

        lobby.players = lobby.players.Append(new() { connId = conn.connectionId, groupId = message.group }).ToArray();
        lobbies[message.lobbyId] = lobby;

        if (_serverInstances.TryGetValue(message.lobbyId, out var arena))
        {
            arena.ServerJoinPlayer(conn, message.group);
        }

        return true;
    }

    [Server]
    public void ServerLeaveLobby(NetworkConnectionToClient conn)
    {
        var lobbyId = "";
        ArenaLobby lobby = null;
        foreach (var item in lobbies)
        {
            if (item.Value.players.Any(player => player.connId == conn.connectionId))
            {
                lobbyId = item.Key;
                lobby = item.Value;
                break;
            }
        }
        if (lobbyId == "" || lobby == null)
            return;
        lobby.players = lobby.players.Where(player => player.connId != conn.connectionId).ToArray();

        //if (lobby.players.Length == 0)
        //{
        //    ServerDeleteLobby(lobbyId);
        //    return;
        //}
        lobbies[lobbyId] = lobby;
        if (_serverInstances.TryGetValue(lobbyId, out var arena))
            arena.ServerLeavePlayer(conn);
    }

    [Server]
    private void ServerOnDisconnected(NetworkConnectionToClient conn)
    {
        ServerLeaveLobby(conn);
    }

    [Server]
    public string ServerGetStringLobbyInstanceState(string lobbyId)
    {
        return _serverInstances.TryGetValue(lobbyId, out var instance)
            ? (instance.ServerGameStarted ? "started" : "paused")
            : "unknown";
    }

    //[Command]
    //private void CmdLeaveLobby(NetworkConnectionToClient sender = null)
    //{
    //    if (sender != null && sender.isAuthenticated)
    //        ServerLeaveLobby(sender);
    //}

    //[Command]
    //private void CmdAdminStartLobby(NetworkConnectionToClient sender = null)
    //{
    //    if (sender != null && sender.isAuthenticated)
    //    {
    //        foreach (var lobby in lobbies)
    //            if (lobby.Value.ownerIsAdmin && lobby.Value.owner == sender.connectionId)
    //            {
    //                ServerStartLobby(lobby.Key);
    //                return;
    //            }
    //    }
    //}

    //[Command]
    //private void CmdAdminStopLobby(NetworkConnectionToClient sender = null)
    //{
    //    if (sender != null && sender.isAuthenticated)
    //    {
    //        foreach (var lobby in lobbies)
    //            if (lobby.Value.ownerIsAdmin && lobby.Value.owner == sender.connectionId)
    //            {
    //                ServerStopLobby(lobby.Key);
    //                return;
    //            }
    //    }
    //}

    //[Command]
    //private void CmdAdminDeleteLobby(NetworkConnectionToClient sender = null)
    //{
    //    if (sender != null && sender.isAuthenticated)
    //    {
    //        foreach (var lobby in lobbies)
    //            if (lobby.Value.ownerIsAdmin && lobby.Value.owner == sender.connectionId)
    //            {
    //                ServerDeleteLobby(lobby.Key);
    //                return;
    //            }
    //    }
    //}

    //[Client]
    //public void ClientJoinLobby(string lobbyId, int group)
    //{
    //    NetworkClient.Send(new JoinLobbyMessage() { lobbyId = lobbyId, group = group });
    //}

    //[Client]
    //public void ClientCreateLobby(string arenaId, string password, int playersPerTeam, bool ownerIsAdmin)
    //{
    //    NetworkClient.Send(new CreateLobbyMessage() {
    //        arenaId = arenaId,
    //        ownerIsAdmin = ownerIsAdmin,
    //        password = password,
    //        playersPerTeam = playersPerTeam
    //    });
    //}

    //[Client]
    //public void ClientAdminStartLobby()
    //{
    //    CmdAdminStartLobby();
    //}

    //[Client]
    //public void ClientAdminStopLobby()
    //{
    //    CmdAdminStopLobby();
    //}

    //[Client]
    //public void ClientAdminDeleteLobby()
    //{
    //    CmdAdminDeleteLobby();
    //}

    //[Client]
    //public void ClientLeaveLobby()
    //{
    //    CmdLeaveLobby();
    //}

    [Serializable]
    public struct CreateLobbyMessage : NetworkMessage
    {
        public string lobbyId;
        public string arenaId;
    }

    [Serializable]
    public struct JoinLobbyMessage : NetworkMessage
    {
        public string lobbyId;
        public int group;
    }

    public class ArenaLobby
    {
        public string lobbyId;
        public string arenaId;
        public Player[] players;
        public PlayerGroup[] groups;
        public struct PlayerGroup
        {
            public string teamId;
            public float respawn;
        }
        public struct Player
        {
            public int connId;
            public int groupId;
        }
    }
}