using System;
using System.Linq;
using Mirror;
using UnityEngine;
public class NetworkTank : NetworkBehaviour
{
    public static NetworkTank LocalPlayer;
    public static event Action<NetworkTank> OnLocalPlayerSpawn;
    public static event Action OnLocalPlayerDestroyed;
    public static event Action OnLocalPlayerDead;

    [Header("Components")]
    [SerializeField]
    private TankControlManager controlManager;
    [SerializeField]
    private TankParameters tankParameters;
    [SerializeField]
    private TankShoot tankShoot;
    [SerializeField]
    private TankSpecial tankSpecial;
    [SerializeField]
    private TankMovement tankMovement;
    [SerializeField]
    private TankStatusEffects tankEffects;
    [SerializeField]
    private TankDeath tankDeath;
    [SerializeField]
    private Rigidbody2D tankRigidbody;

    [Header("Local Player")]
    [SerializeField]
    private GameObject[] inactiveIfNotLocalPlayer;
    [SerializeField]
    private GameObject[] enableOnLocalPlayer;

    [Header("Additional Info")]
    [SerializeField, Tooltip("Describes the size of the tank sprites to correctly display some visual effects")]
    private float tankVisualSize = 1;
    [SerializeField]
    private bool usesCursor = false;
    [SerializeField]
    private bool showDirectionLine = true;

    private string tankId;

    public string TankId => tankId;
    public TankControlManager ControlManager => controlManager;
    public TankParameters Parameters => tankParameters;
    public TankShoot Shoot => tankShoot;
    public TankSpecial Special => tankSpecial;
    public TankMovement Movement => tankMovement;
    public TankStatusEffects StatusEffects => tankEffects;
    public TankDeath Death => tankDeath;
    public Rigidbody2D Rigidbody => tankRigidbody;

    public float VisualSize => tankVisualSize;
    public bool UsesCursor => usesCursor;
    public bool ShowDirectionLine => showDirectionLine;

    private bool _wasLocalPlayer = false;

    public override void OnStartClient()
    {
        var currentTank = GameTanksManager.Singleton.tanks.FirstOrDefault(tank => tank.PrefabAssetId == netIdentity.assetId);
        if (currentTank != default)
            tankId = currentTank.id;
        _wasLocalPlayer = isLocalPlayer;
        if (isLocalPlayer)
        {
            LocalPlayer = this;
            OnLocalPlayerSpawn?.Invoke(LocalPlayer);
            tankDeath.OnDeath += OnLocalPlayerDeath;
            foreach (var obj in enableOnLocalPlayer)
                if (obj)
                    obj.SetActive(true);
        }
        else
            foreach (var obj in inactiveIfNotLocalPlayer)
                if (obj)
                    obj.SetActive(false);
        if (isClientOnly)
            tankRigidbody.excludeLayers = ~0;
    }

    public override void OnStopClient()
    {
        if (isLocalPlayer)
            OnLocalPlayerDestroyed?.Invoke();
    }

    public override void OnStartServer()
    {
        var currentTank = GameTanksManager.Singleton.tanks.FirstOrDefault(tank => tank.PrefabAssetId == netIdentity.assetId);
        if (currentTank != default)
            tankId = currentTank.id;
    }

    private void OnLocalPlayerDeath()
    {
        OnLocalPlayerDead?.Invoke();
    }

    private void Update()
    {
        if (isLocalPlayer != _wasLocalPlayer)
        {
            _wasLocalPlayer = isLocalPlayer;
            if (_wasLocalPlayer)
            {
                LocalPlayer = this;
                OnLocalPlayerSpawn?.Invoke(LocalPlayer);
                tankDeath.OnDeath += OnLocalPlayerDeath;
                foreach (var obj in enableOnLocalPlayer)
                    if (obj)
                        obj.SetActive(true);
            }
            else
            {
                if (LocalPlayer == this)
                    LocalPlayer = null;
                tankDeath.OnDeath -= OnLocalPlayerDeath;
                foreach (var obj in inactiveIfNotLocalPlayer)
                    if (obj)
                        obj.SetActive(false);
            }
        }
    }
}
