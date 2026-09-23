using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
public class NetworkArenaPlayerStats : NetworkBehaviour
{

    public NetworkArena arena;

    [Space]

    [Header("Kills Buffer")]
    public int remeberKills = 10;

    [Header("Assist Rules")]
    public float assistMaxTime = 15f;
    [Range(0, 1)]
    public float assistMinDamage = 0.2f;
    public uint assistPerKillLimit = 2;


    [HideInInspector]
    public readonly SyncDictionary<int, PlayerStats> playerStats = new();
    [HideInInspector]
    public readonly SyncList<KillData> kills = new();

    [HideInInspector]
    public readonly List<DamageData> serverDamageBuffer = new();


    public override void OnStartServer()
    {
        GameTanksManager.Singleton.ServerOnPlayerTankSpawn += OnPlayerTankSpawn;
    }

    public override void OnStopServer()
    {
        if (GameTanksManager.Singleton)
            GameTanksManager.Singleton.ServerOnPlayerTankSpawn -= OnPlayerTankSpawn;
    }

    public void OnDestroy()
    {
        if (GameTanksManager.Singleton)
            GameTanksManager.Singleton.ServerOnPlayerTankSpawn -= OnPlayerTankSpawn;
    }

    private void OnPlayerTankSpawn(NetworkConnectionToClient conn, NetworkTank tank)
    {
        if (!arena.ServerGameStarted)
            return;
        if (arena.serverPlayersToGroups.ContainsKey(conn.connectionId))
        {
            var stats = playerStats.GetValueOrDefault(conn.connectionId, new());
            stats.playerName = conn.Username();
            stats.teamId = arena.serverSpawnGroups[arena.serverPlayersToGroups[conn.connectionId]].teamId;
            stats.tankId = tank.TankId;
            playerStats[conn.connectionId] = stats;
            tank.Parameters.ServerOnDamageTakenFromPlayer += (connId, damage) => OnTankDamageDealt(tank, damage, connId);
            tank.Parameters.ServerOnHealTakenFromPlayer += (connId, heal) => OnTankHealDealt(tank, heal, connId);
        }
    }

    private void OnTankDamageDealt(NetworkTank targetTank, ushort damage, int fromPlayerConnId)
    {
        if (!arena.ServerGameStarted)
            return;
        if (!arena.serverPlayersToGroups.ContainsKey(targetTank.connectionToClient.connectionId))
            return;
        if (!arena.serverPlayersToGroups.ContainsKey(fromPlayerConnId))
            return;

        if (targetTank.connectionToClient == null)
            return;
        var targetConnId = targetTank.connectionToClient.connectionId;
        if (!arena.serverPlayersToGroups.ContainsKey(targetConnId))
            return;

        if (!playerStats.ContainsKey(targetConnId))
            playerStats[targetConnId] = new();
        if (!playerStats.ContainsKey(fromPlayerConnId))
            playerStats[fromPlayerConnId] = new();

        serverDamageBuffer.Add(new()
        {
            fromPlayer = fromPlayerConnId,
            toPlayer = targetConnId,
            toTankNetId = targetTank.netId,
            damage = damage,
            timestamp = NetworkTime.time
        });

        var attacker = playerStats[fromPlayerConnId];
        attacker.damage += damage;
        if (targetTank.Parameters.CurrentHp == 0)
            attacker.kills++;
        playerStats[fromPlayerConnId] = attacker;

        if (targetTank.Parameters.CurrentHp == 0)
        {
            var target = playerStats[targetConnId];
            target.deaths++;
            playerStats[targetConnId] = target;
        }

        var assistPlayers = new List<int>();
        if (targetTank.Parameters.CurrentHp == 0)
        {

            var minDamage = targetTank.Parameters.MaxHp * assistMinDamage;
            var threshold = NetworkTime.time - assistMaxTime;
            var damageByPlayer = serverDamageBuffer
                .Where(damageData =>
                    damageData.timestamp > threshold
                    && damageData.toPlayer == targetConnId
                    && damageData.fromPlayer != fromPlayerConnId
                )
                .GroupBy(d => d.fromPlayer)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    TotalDamage = g.Sum(d => d.damage)
                })
                .Where(g => g.TotalDamage > minDamage)
                .OrderByDescending(g => g.TotalDamage);
            assistPlayers.AddRange(damageByPlayer.Take(2).Select(g => g.PlayerId));

            foreach (var assistPlayerId in assistPlayers)
            {
                var assistPlayer = playerStats[assistPlayerId];
                assistPlayer.assists++;
                playerStats[assistPlayerId] = assistPlayer;
            }
        }

        if (targetTank.Parameters.CurrentHp == 0)
        {
            kills.Add(new()
            {
                index = kills.Count > 0 ? kills.Last().index + 1 : 0,
                timestamp = NetworkTime.time,
                killedPlayer = targetConnId,
                killedByPlayer = fromPlayerConnId,
                assistPlayers = assistPlayers.ToArray()
            });
            if (kills.Count > remeberKills)
            {
                int excess = kills.Count - remeberKills;
                kills.TryRemoveElementsInRange(0, excess, out var error);
            }
        }
    }

    private void OnTankHealDealt(NetworkTank targetTank, ushort heal, int fromPlayerConnId)
    {
        if (!arena.ServerGameStarted)
            return;
        if (!arena.serverPlayersToGroups.ContainsKey(targetTank.connectionToClient.connectionId))
            return;
        if (!arena.serverPlayersToGroups.ContainsKey(fromPlayerConnId))
            return;

        if (targetTank.connectionToClient == null)
            return;
        var targetConnId = targetTank.connectionToClient.connectionId;
        if (!arena.serverPlayersToGroups.ContainsKey(targetConnId))
            return;

        if (!playerStats.ContainsKey(fromPlayerConnId))
            playerStats[fromPlayerConnId] = new();

        var stats = playerStats[fromPlayerConnId];
        stats.heal += heal;
        playerStats[fromPlayerConnId] = stats;
    }

    [Serializable]
    public struct DamageData
    {
        public double timestamp;
        public ushort damage;
        public int fromPlayer;
        public int toPlayer;
        public uint toTankNetId;
    }

    [Serializable]
    public struct PlayerStats
    {
        public string teamId;
        public string playerName;
        public string tankId;
        public uint kills;
        public uint assists;
        public uint deaths;
        public uint damage;
        public uint heal;
    }

    [Serializable]
    public struct KillData
    {
        public int index;
        public double timestamp;
        public int killedPlayer;
        public int killedByPlayer;
        public int[] assistPlayers;
    }
}