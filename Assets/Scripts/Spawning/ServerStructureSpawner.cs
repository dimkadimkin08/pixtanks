using System.Collections.Generic;
using Mirror;
using UnityEngine;
public class ServerStructureSpawner : NetworkBehaviour
{
    public static readonly List<ServerStructureSpawner> spawners = new();

    [SerializeField]
    private string structureId;
    [SerializeField]
    private float minSpawnInterval = 4;
    [SerializeField]
    private float maxSpawnInterval = 8;
    [SerializeField]
    private ushort maxStructures = 5;

    public readonly List<NetworkStructure> spawnedStructures = new();
    private int currentlySpawningStructures = 0;

    private ushort worldSizeMultiplier = 1;

    private float _timeUntilSpawn = 100;

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

        spawnedStructures.RemoveAll(structure => !structure);
        if (spawnedStructures.Count + currentlySpawningStructures >= maxStructures * worldSizeMultiplier)
            return;

        _timeUntilSpawn -= Time.fixedDeltaTime;
        if (_timeUntilSpawn > 0)
            return;

        currentlySpawningStructures++;
        var spawnPosition = NetworkMap.Singleton.SpawnPoints.RandomElement();
        GameStructuresManager.Singleton.ServerSpawnStructureLandingCapsule(structureId, spawnPosition,
            callback: (structure) =>
            {
                spawnedStructures.Add(structure);
                currentlySpawningStructures--;
            },
            onError: () =>
            {
                currentlySpawningStructures--;
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
        foreach (var structure in spawnedStructures)
            structure.ServerKillStructure(preventLootDrop: !lootEnabled);
        spawnedStructures.Clear();
    }
}

