using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
public class NetworkArena : NetworkBehaviour
{

    [SerializeField]
    private NetworkArenaLoader arenaLoader;
    [SerializeField]
    private NetworkArenaPlayerStats stats;

    [HideInInspector]
    public readonly List<ServerSpawnGroup> serverSpawnGroups = new();
    public UnityEvent serverOnStartGame = new();
    public UnityEvent serverOnStopGame = new();

    public bool ServerGameStarted { get; private set; } = false;

    [HideInInspector]
    public readonly Dictionary<int, int> serverPlayersToGroups = new();
    [HideInInspector]
    public readonly Dictionary<int, int> serverGroupsToSpawnIndex = new();


    private void Start()
    {
        if (NetworkServer.active)
            NetworkServer.OnDisconnectedEvent += OnClientDisconnect;
    }

    public void OnDestroy()
    {
        NetworkServer.OnDisconnectedEvent -= OnClientDisconnect;
    }

    private void OnClientDisconnect(NetworkConnectionToClient conn)
    {
        ServerLeavePlayer(conn);
    }

    [Server]
    private void ServerInitPlayerConnection(NetworkConnectionToClient conn, int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= serverSpawnGroups.Count())
            return;
        var spawnGroup = serverSpawnGroups[groupIndex];
        var spawnIndex = serverGroupsToSpawnIndex.TryGetValue(groupIndex, out var index) ? index + 1 : 0;
        if (spawnIndex >= spawnGroup.spawnPoints.Length)
            spawnIndex = 0;
        serverGroupsToSpawnIndex[groupIndex] = spawnIndex;
        var spawnPos = spawnGroup.spawnPoints[spawnIndex].position;
        var spawnAngle = spawnGroup.spawnPoints[spawnIndex].eulerAngles.z;
        GameTanksManager.Singleton.SetPlayerSpawnPoint(conn, spawnPos, spawnGroup.teamId, spawnGroup.respawnDelay, spawnAngle);
        GameTanksManager.Singleton.InterruptPlayer(conn);
        GameTanksManager.Singleton.TriggerPlayerRespawn(conn);
    }

    [Server]
    private void ServerResetPlayerConnection(NetworkConnectionToClient conn)
    {
        GameTanksManager.Singleton.ClearPlayerSpawnPoint(conn);
        if (ServerGameStarted)
            GameTanksManager.Singleton.InterruptPlayer(conn);
    }

    [Server]
    public bool ServerJoinPlayer(NetworkConnectionToClient conn, int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= serverSpawnGroups.Count())
            return false;

        serverPlayersToGroups[conn.connectionId] = groupIndex;

        if (ServerGameStarted)
            ServerInitPlayerConnection(conn, groupIndex);

        return true;
    }

    [Server]
    public void ServerLeavePlayer(NetworkConnectionToClient conn)
    {
        if (serverPlayersToGroups.ContainsKey(conn.connectionId))
        {
            ServerResetPlayerConnection(conn);
        }
        serverPlayersToGroups.Remove(conn.connectionId);
        stats.playerStats.Remove(conn.connectionId);
    }

    [Server]
    public void ServerClearAllSlots()
    {
        var connectionIds = serverPlayersToGroups.Keys.ToArray();
        foreach (var connId in connectionIds)
            if (NetworkServer.connections.TryGetValue(connId, out var conn))
                ServerLeavePlayer(conn);
        if (ServerGameStarted)
            ServerStopGame();
    }

    [Server]
    public void ServerStartGame()
    {
        if (ServerGameStarted)
            return;
        serverOnStartGame?.Invoke();
        foreach (var (connId, groupIndex) in serverPlayersToGroups)
            if (NetworkServer.connections.TryGetValue(connId, out var conn))
                ServerInitPlayerConnection(conn, groupIndex);
        stats.playerStats.Clear();
        stats.kills.Clear();
        stats.serverDamageBuffer.Clear();
        ServerGameStarted = true;
    }

    [Server]
    public void ServerStopGame()
    {
        if (!ServerGameStarted)
            return;
        serverOnStopGame?.Invoke();
        foreach (var connId in serverPlayersToGroups.Keys)
            if (NetworkServer.connections.TryGetValue(connId, out var conn))
                ServerResetPlayerConnection(conn);
        stats.playerStats.Clear();
        stats.kills.Clear();
        stats.serverDamageBuffer.Clear();
        ServerGameStarted = false;
    }

    [Server]
    public void ServerDestroyArena()
    {
        ServerClearAllSlots();
        arenaLoader.Cleanup();
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    public void ServerInitArena(string arenaId)
    {
        arenaLoader.ServerLoadArena(arenaId);
    }


    public class ServerSpawnGroup
    {
        public string teamId;
        public float respawnDelay = -1;
        public Transform[] spawnPoints;
    }

}