using System.Collections.Generic;
using Mirror;
using UnityEngine;
public class ServerTankSpawner : NetworkBehaviour
{
    public static readonly List<ServerTankSpawner> spawners = new();

    [SerializeField]
    private LandingCapsule landingCapsulePrefab;
    [SerializeField]
    private string teamId;
    [SerializeField]
    private string[] tankIds;
    [SerializeField]
    private float minSpawnInterval = 4;
    [SerializeField]
    private float maxSpawnInterval = 8;
    [SerializeField]
    private ushort maxTanks = 5;

    public readonly List<NetworkTank> spawnedTanks = new();
    private int currentlySpawningTanks = 0;

    private ushort worldSizeMultiplier = 1;

    private float _timeUntilSpawn;

    public bool spawnerEnabled = true;

    public override void OnStartServer()
    {
        worldSizeMultiplier = GameNetworkManager.Singleton.serverConfig.GetConfigParams().worldSize;
        RestartSpawnTimer();
        spawners.Add(this);
    }

    public override void OnStopServer()
    {
        spawners.Remove(this);
    }

    private void FixedUpdate()
    {
        if (!isServer || !spawnerEnabled)
            return;

        spawnedTanks.RemoveAll(tank => !tank);
        if (spawnedTanks.Count + currentlySpawningTanks >= maxTanks * worldSizeMultiplier)
            return;

        _timeUntilSpawn -= Time.fixedDeltaTime;
        if (_timeUntilSpawn > 0)
            return;

        currentlySpawningTanks++;
        var tankId = tankIds.RandomElement();
        var spawnPosition = NetworkMap.Singleton.SpawnPoints.RandomElement();
        GameTanksManager.Singleton.ServerSpawnBotLandingCapsule(
            tankId: tankId,
            teamId: teamId,
            position: spawnPosition,
            callback: (tank) =>
            {
                spawnedTanks.Add(tank);
                currentlySpawningTanks--;
            },
            onError: () =>
            {
                currentlySpawningTanks--;
            }
        );

        RestartSpawnTimer();
    }

    private void RestartSpawnTimer()
    {
        _timeUntilSpawn = Random.Range(minSpawnInterval, maxSpawnInterval) / worldSizeMultiplier;
    }

    public void ClearSpawner(bool lootEnabled)
    {
        foreach (var tank in spawnedTanks)
            tank.Death.ServerKillTank(preventLootDrop: !lootEnabled);
        spawnedTanks.Clear();
    }
}