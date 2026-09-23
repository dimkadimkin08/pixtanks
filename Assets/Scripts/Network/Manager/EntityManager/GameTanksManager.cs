using Mirror;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using static GameTanksManager;
public class GameTanksManager : MonoBehaviour
{

    public static GameTanksManager Singleton;

    [Header("Teams")]
    public Team[] teams;
    public string defaultPlayerTeam;

    [Header("Tanks")]
    public Tank[] tanks;
    public string defaultPlayerTank;

    [Header("Landing Capsule")]
    public LandingCapsule landingCapsulePrefab;

    [Header("Player Respawn")]
    [SerializeField]
    private bool respawnPlayers = true;
    [SerializeField]
    private float playerRespawnDelay = 5;

    public readonly List<NetworkTank> serverTankInstances = new();

    private readonly Dictionary<int, SavedSpawnPoint> savedSpawnPoints = new();
    private readonly Dictionary<int, LandingCapsule> serverPlayerLandingCapsules = new();
    private readonly Dictionary<int, DeadPlayerInfo> deadPlayers = new();

    [HideInInspector]
    public event Action<NetworkConnectionToClient, NetworkTank> ServerOnPlayerTankSpawn;

    private void Awake()
    {
        Singleton = this;
    }

    private void OnDestroy()
    {
        ServerOnPlayerTankSpawn = null;
    }

    private void Update()
    {
        var currentTime = NetworkTime.time;
        var respawnedPlayers = new List<int>();

        foreach (var (connId, playerInfo) in deadPlayers)
        {
            if (playerInfo.respawnTime <= currentTime)
            {
                if (NetworkServer.connections.TryGetValue(connId, out var conn) && conn is { isAuthenticated: true, isReady: true })
                {

                    if (ServerSpawnPlayerLandingCapsule(conn))
                    {
                        respawnedPlayers.Add(connId);
                    }
                }
            }
        }

        foreach (var connId in respawnedPlayers)
        {
            deadPlayers.Remove(connId);
        }
    }

    private SpawnPos GetPlayerSpawnPoint(NetworkConnectionToClient conn)
    {
        if (savedSpawnPoints.ContainsKey(conn.connectionId))
            return new() { pos = savedSpawnPoints[conn.connectionId].position, angle = savedSpawnPoints[conn.connectionId].angle };
        Vector2 spawnPoint = Vector2.zero;
        var spawnPoints = NetworkMap.Singleton.SpawnPoints;
        if (spawnPoints.Length > 0)
            spawnPoint = spawnPoints.RandomElement();
        else
            Debug.Log("No spawn points found. Spawning player at 0,0");
        return new() { pos = spawnPoint };
    }

    [Server]
    private void ServerOnPlayerTankDeath(NetworkTank tank)
    {
        if (tank.connectionToClient == null)
            return;

        SetDeadPlayerInfo(tank.connectionToClient);
    }

    [Server]
    private void SetDeadPlayerInfo(NetworkConnectionToClient conn)
    {
        if (respawnPlayers)
        {
            var respawnDelay = playerRespawnDelay;

            if (savedSpawnPoints.TryGetValue(conn.connectionId, out var spawnPoint) && spawnPoint.respawnDelay >= 0)
            {
                respawnDelay = spawnPoint.respawnDelay;
            }

            deadPlayers[conn.connectionId] = new()
            {
                deathTime = NetworkTime.time,
                respawnTime = NetworkTime.time + respawnDelay
            };
        }
    }

    [Server]
    public void RequestPlayerSpawn(NetworkConnectionToClient conn)
    {
        deadPlayers[conn.connectionId] = new()
        {
            deathTime = NetworkTime.time,
            respawnTime = NetworkTime.time + 0.5f
        };
    }

    [Server]
    public void InterruptPlayer(NetworkConnectionToClient conn)
    {

        serverPlayerLandingCapsules.Remove(conn.connectionId);

        if (conn.identity)
        {
            if (conn.identity.TryGetComponent(out NetworkTank tank))
            {
                tank.Death.ServerKillTank(preventLootDrop: true);
                SetDeadPlayerInfo(conn);
            }
            else
            {
                NetworkServer.DestroyPlayerForConnection(conn);
                SetDeadPlayerInfo(conn);
            }
        }
        else
        {
            SetDeadPlayerInfo(conn);
        }
    }

    [Server]
    public void TriggerPlayerRespawn(NetworkConnectionToClient conn)
    {
        if (deadPlayers.ContainsKey(conn.connectionId))
            deadPlayers[conn.connectionId].respawnTime = Math.Min(NetworkTime.time, deadPlayers[conn.connectionId].respawnTime);
    }

    [Server]
    public void OnPlayerDisconnect(NetworkConnectionToClient conn)
    {
        deadPlayers.Remove(conn.connectionId);
        serverPlayerLandingCapsules.Remove(conn.connectionId);
        if (NetworkShop.Singleton)
            NetworkShop.Singleton.selectedPlayerTanks.Remove(conn.connectionId);
        savedSpawnPoints.Remove(conn.connectionId);
    }

    [Server]
    public void ServerSetPlayerSavedTank(NetworkConnectionToClient conn, string tankId)
    {
        if (NetworkShop.Singleton)
            NetworkShop.Singleton.selectedPlayerTanks[conn.connectionId] = tankId;
    }

    [Server]
    public void SetPlayerSpawnPoint(NetworkConnectionToClient conn, Vector2 spawnPoint, string teamId, float respawnDelay = -1, float angle = 0)
    {
        savedSpawnPoints[conn.connectionId] = new() { position = spawnPoint, teamId = teamId, respawnDelay = respawnDelay, angle = angle };
    }

    [Server]
    public void ClearPlayerSpawnPoint(NetworkConnectionToClient conn)
    {
        savedSpawnPoints.Remove(conn.connectionId);
    }

    [Server]
    public bool ServerSpawnPlayerLandingCapsule(NetworkConnectionToClient conn)
    {
        if (!conn.isAuthenticated)
            return false;

        var savedTeamId = savedSpawnPoints.TryGetValue(conn.connectionId, out var savedSpawnPoint) ? savedSpawnPoint.teamId : null;

        if (serverPlayerLandingCapsules.TryGetValue(conn.connectionId, out var otherLandingCapsule) && otherLandingCapsule)
            return false;

        var spawnPoint = GetPlayerSpawnPoint(conn);
        var landingCapsule = Instantiate(landingCapsulePrefab, spawnPoint.pos, Quaternion.Euler(0, 0, spawnPoint.angle));
        serverPlayerLandingCapsules[conn.connectionId] = landingCapsule;
        serverPlayerLandingCapsules[conn.connectionId].ServerInit(new LandingCapsule.PlayerTankInitData
        {
            disableImpulse = savedTeamId != null,
            connectionId = conn.connectionId,
            teamId = savedTeamId ?? defaultPlayerTeam,
            callback = (tank) =>
            {
                serverPlayerLandingCapsules.Remove(conn.connectionId);
            },
            onError = () =>
            {
                serverPlayerLandingCapsules.Remove(conn.connectionId);
            }
        });
        if (!conn.identity)
            NetworkServer.AddPlayerForConnection(conn, landingCapsule.gameObject);
        else
            NetworkServer.ReplacePlayerForConnection(conn, landingCapsule.gameObject, ReplacePlayerOptions.KeepActive);
        return true;
    }

    [Server]
    public bool ServerSpawnPlayerTank(NetworkConnectionToClient conn)
    {
        if (!conn.isAuthenticated)
            return false;

        if (serverPlayerLandingCapsules.TryGetValue(conn.connectionId, out var otherLandingCapsule) && otherLandingCapsule)
            return false;

        var spawnPoint = GetPlayerSpawnPoint(conn);
        return ServerSpawnPlayerTank(conn, spawnPoint.pos, spawnPoint.angle, true);
    }

    [Server]
    public bool ServerSpawnPlayerTank(NetworkConnectionToClient conn, Vector2 position, float angle = 0, bool checkCapsules = true)
    {
        if (!conn.isAuthenticated)
            return false;

        var savedTeamId = savedSpawnPoints.TryGetValue(conn.connectionId, out var savedSpawnPoint) ? savedSpawnPoint.teamId : null;

        if (checkCapsules && serverPlayerLandingCapsules.TryGetValue(conn.connectionId, out var otherLandingCapsule) && otherLandingCapsule)
            return false;

        var defaultTeam = GetTeam(savedTeamId ?? defaultPlayerTeam);
        var defaultTank = GetTank(NetworkShop.Singleton && NetworkShop.Singleton.selectedPlayerTanks.TryGetValue(conn.connectionId, out var plrTank) ? plrTank : defaultPlayerTank);
        if (defaultTeam == null || defaultTank == null)
            return false;

        var tankObject = Instantiate(defaultTank.prefab, position, Quaternion.Euler(0, 0, angle));

        if (!tankObject.TryGetComponent(out NetworkTank tankComponent))
        {
            Destroy(tankObject);
            return false;
        }

        tankComponent.Parameters.DisplayName = conn.Username();
        tankComponent.Parameters.TeamId = defaultTeam.id;

        if (!conn.identity)
            NetworkServer.AddPlayerForConnection(conn, tankObject);
        else
            NetworkServer.ReplacePlayerForConnection(conn, tankObject, ReplacePlayerOptions.KeepActive);

        tankComponent.ControlManager.serverCurrentController = TankControlManager.TankController.Player;

        tankComponent.Death.OnDeath += () => ServerOnPlayerTankDeath(tankComponent);

        serverTankInstances.RemoveAll(tank => !tank);
        serverTankInstances.Add(tankComponent);

        ServerOnPlayerTankSpawn?.Invoke(conn, tankComponent);

        return true;
    }

    [Server]
    public bool ServerReplacePlayerTank(NetworkConnectionToClient conn, string tankId)
    {
        if (!conn.identity || !conn.isAuthenticated)
            return false;

        if (!conn.identity.TryGetComponent(out NetworkTank oldTank) || oldTank.Death.IsDead)
            return false;

        var tank = GetTank(tankId);

        if (tank == null)
            return false;

        var tankObject = Instantiate(tank.prefab, oldTank.Rigidbody.position, Quaternion.Euler(0, 0, oldTank.Rigidbody.rotation));

        tankObject.name += $"[connId={conn.connectionId}]";

        if (!tankObject.TryGetComponent(out NetworkTank tankComponent))
        {
            Destroy(tankObject);
            return false;
        }

        var displayName = oldTank.Parameters.DisplayName;
        var teamId = oldTank.Parameters.TeamId;
        var gears = oldTank.Parameters.Gears;

        tankComponent.Parameters.DisplayName = displayName;
        tankComponent.Parameters.TeamId = teamId;
        tankComponent.Parameters.Gears = gears;

        NetworkServer.ReplacePlayerForConnection(conn, tankObject, ReplacePlayerOptions.Destroy);

        tankComponent.ControlManager.serverCurrentController = TankControlManager.TankController.Player;

        tankComponent.Death.OnDeath += () => ServerOnPlayerTankDeath(tankComponent);

        serverTankInstances.RemoveAll(tank => !tank);
        serverTankInstances.Add(tankComponent);

        ServerOnPlayerTankSpawn?.Invoke(conn, tankComponent);

        return true;
    }

    [Server]
    public void ServerSpawnBotLandingCapsule(string teamId, string tankId, Vector2 position, Action<NetworkTank> callback,
        Action onError, bool noAi = false, bool disableImpulse = false)
    {
        var landingCapsule = GameObject.Instantiate(landingCapsulePrefab, position, Quaternion.identity);
        landingCapsule.ServerInit(new LandingCapsule.BotTankInitData
        {
            disableImpulse = disableImpulse,
            noAi = noAi,
            tankId = tankId,
            teamId = teamId,
            callback = callback,
            onError = onError
        });
        NetworkServer.Spawn(landingCapsule.gameObject);
    }

    [Server]
    public NetworkTank ServerSpawnTank(string teamId, string tankId, Vector2 position, bool noAi = false)
    {
        var team = GetTeam(teamId);
        var tank = GetTank(tankId);
        if (team == null || tank == null)
            return null;

        var tankObject = Instantiate(tank.prefab, position, Quaternion.identity);

        if (tankObject.TryGetComponent(out NetworkTank tankComponent))
        {
            tankComponent.Parameters.TeamId = team.id;
            if (!noAi)
                tankComponent.ControlManager.serverCurrentController = TankControlManager.TankController.AI;
        }
        else
        {
            Destroy(tankObject);
            return null;
        }

        NetworkServer.Spawn(tankObject);

        serverTankInstances.RemoveAll(tank => !tank);
        serverTankInstances.Add(tankComponent);

        return tankComponent;
    }

    public IEnumerable<NetworkTank> ServerGetTankInstances()
    {
        serverTankInstances.RemoveAll(tank => !tank);
        return serverTankInstances;
    }

    public ReadOnlyDictionary<int, LandingCapsule> ServerGetPlayerLandingCapsuleInstances()
    {
        foreach (var pair in serverPlayerLandingCapsules.Where(pair => !pair.Value).ToList())
            serverPlayerLandingCapsules.Remove(pair.Key);
        return new(serverPlayerLandingCapsules);
    }

    public Tank GetTank(string tankId)
    {
        return tanks.FirstOrDefault(tank => tank.id == tankId);
    }

    public Team GetTeam(string teamId)
    {
        return teams.FirstOrDefault(team => team.id == teamId);
    }

    public struct SpawnPos
    {
        public Vector2 pos;
        public float angle;
    }

    [Serializable]
    public class DeadPlayerInfo
    {
        public double deathTime;
        public double respawnTime;
    }

    [Serializable]
    public class SavedSpawnPoint
    {
        public Vector2 position;
        public string teamId;
        public float respawnDelay;
        public float angle;
    }

    [Serializable]
    public class SavePlayerTankParam
    {
        public string[] includeTanks;
        public string tankId;
    }

    [Serializable]
    public class Tank
    {
        private uint _prefabAssetId = 0;
        public string id;
        public GameObject prefab;
        public uint PrefabAssetId
        {
            get
            {
                if (_prefabAssetId == 0 && prefab.TryGetComponent(out NetworkIdentity networkIdentity))
                    _prefabAssetId = networkIdentity.assetId;
                return _prefabAssetId;
            }
        }
    }

    [Serializable]
    public class Team
    {
        public string id;
        public Color color;
        public string[] enemies;
        public LayerMask includeObstacles;
    }

}

