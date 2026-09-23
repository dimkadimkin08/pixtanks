using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class NetworkSingleStructureSpawner : NetworkBehaviour
{
    [SyncVar]
    public bool spawnerEnabled = false;

    [Space]

    [SerializeField]
    private string structureId;
    [SerializeField]
    private Transform spawnPrivot;
    [SyncVar]
    public float respawnDelay = 15;
    [SerializeField]
    private bool waitForStructureToDestroy = true;

    [Space]

    [SerializeField]
    private Image fillIndicatorImage;

    [SyncVar]
    private double nextSpawnTime;

    private bool _serverIsSpawning;
    private LandingCapsule _serverStructureCapsule;
    private NetworkStructure _serverStructure;

    public override void OnStartServer()
    {
        nextSpawnTime = NetworkTime.time + respawnDelay;
    }

    private void Update()
    {
        if (isServer)
        {
            if (!spawnerEnabled)
            {
                if (_serverStructure && !_serverStructure.ServerIsDead())
                {
                    _serverStructure.ServerKillStructure(preventLootDrop: true);
                }
            }
            if (spawnerEnabled)
            {
                if (nextSpawnTime == 0 && !_serverIsSpawning && (!waitForStructureToDestroy || !_serverStructure))
                {
                    nextSpawnTime = NetworkTime.time + respawnDelay;
                }
                if (nextSpawnTime != 0 && nextSpawnTime - NetworkTime.time < 0)
                {
                    if (_serverStructure && !_serverStructure.ServerIsDead())
                        _serverStructure.ServerKillStructure();
                    _serverIsSpawning = true;
                    _serverStructureCapsule = GameStructuresManager.Singleton.ServerSpawnStructureLandingCapsule(
                        structureId,
                        spawnPrivot.position,
                        structure =>
                        {
                            _serverStructure = structure;
                            _serverIsSpawning = false;
                        },
                        () =>
                        {
                            _serverIsSpawning = false;
                        },
                        disableImpulse: true
                    );
                    nextSpawnTime = 0;
                }
            }
        }
        if (!AppPlatform.IsHeadless && fillIndicatorImage)
        {
            var progress = !spawnerEnabled ? 0 : 1 - Mathf.Clamp01((float)((nextSpawnTime - NetworkTime.time) / respawnDelay));
            fillIndicatorImage.fillAmount = progress != 1 ? progress : 0;
        }
    }

    [Server]
    public void ServerResetAndSetEnabled(bool spawnerEnabledSelf)
    {
        if (_serverStructure && !_serverStructure.ServerIsDead())
        {
            _serverStructure.ServerKillStructure(preventLootDrop: true);
        }
        if (_serverStructureCapsule)
        {
            _serverStructureCapsule.DestroyNetObject();
        }
        _serverIsSpawning = false;
        spawnerEnabled = spawnerEnabledSelf;
        if (spawnerEnabledSelf)
            nextSpawnTime = NetworkTime.time + respawnDelay;
    }
}