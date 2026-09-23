using Mirror;
using UnityEngine;

public class NetworkMap : NetworkBehaviour
{
    public static NetworkMap Singleton;

    [Header("Generator")]
    [SerializeField]
    private MapGenerator mapGenerator;

    [SyncVar(hook = nameof(HookMapData))]
    private MapData mapData;

    public Vector2[] SpawnPoints => mapGenerator._spawnPoints;

    public NetworkMap()
    {
        Singleton = this;
    }

    private void Awake()
    {
        Singleton = this;
    }


    public override void OnStartServer()
    {
        mapData = new()
        {
            seed = mapGenerator.LastSeed,
            sizeMultiplier = mapGenerator.LastSizeMultiplier
        };
    }

    private void HookMapData(MapData old, MapData current)
    {
        if (!isServer)
            mapGenerator.WriteMap(current.seed, current.sizeMultiplier);
    }

    [System.Serializable]
    public struct MapData
    {
        public ushort seed;
        public ushort sizeMultiplier;
    }

}