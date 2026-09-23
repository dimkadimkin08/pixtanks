using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
public class GameStructuresManager : MonoBehaviour
{

    public static GameStructuresManager Singleton;

    [Header("Landing Capsule")]
    public LandingCapsule landingCapsulePrefab;

    [Header("Structures")]
    public StructureAsset[] structures;

    private readonly List<NetworkStructure> serverStructureInstances = new();

    private void Awake()
    {
        Singleton = this;
    }

    public StructureAsset GetStructure(string structureId)
    {
        return structures.FirstOrDefault(structure => structure.id == structureId);
    }

#nullable enable

    [Server]
    public LandingCapsule? ServerSpawnStructureLandingCapsule(string structureId, Vector2 position, Action<NetworkStructure> callback,
        Action onError, bool disableImpulse = false)
    {
        if (GetStructure(structureId) == default)
            return null;
        var landingCapsule = Instantiate(landingCapsulePrefab, position, Quaternion.identity);
        landingCapsule.ServerInit(new LandingCapsule.StructureInitData
        {
            disableImpulse = disableImpulse,
            structureId = structureId,
            callback = callback,
            onError = onError
        });
        NetworkServer.Spawn(landingCapsule.gameObject);
        return landingCapsule;
    }

#nullable disable

    [Server]
    public NetworkStructure ServerSpawnStructure(string structureId, Vector2 position)
    {
        var structureAsset = GetStructure(structureId);
        if (structureAsset == default)
            return null;

        var structureObject = Instantiate(structureAsset.prefab, position, Quaternion.identity);
        if (!structureObject.TryGetComponent(out NetworkStructure structureComponent))
        {
            Destroy(structureObject);
            return null;
        }
        NetworkServer.Spawn(structureObject);

        serverStructureInstances.RemoveAll(structure => !structure);
        serverStructureInstances.Add(structureComponent);

        return structureComponent;
    }

    public IEnumerable<NetworkStructure> ServerGetStructureInstances()
    {
        serverStructureInstances.RemoveAll(structure => !structure);
        return serverStructureInstances;
    }

    [Serializable]
    public class StructureAsset
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
}