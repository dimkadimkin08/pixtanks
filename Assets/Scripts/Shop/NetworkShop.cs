using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;
public class NetworkShop : NetworkBehaviour
{
    public static string CurrentSelectedTankId => Singleton && Singleton.selectedPlayerTanks.TryGetValue(GameNetworkAuthenticator.LocalPlayerConnId, out var plrTank)
        ? plrTank
        : (GameTanksManager.Singleton ? GameTanksManager.Singleton.defaultPlayerTank : "");
    public static string CurrentTankId => NetworkTank.LocalPlayer && !NetworkTank.LocalPlayer.Death.IsDead ? NetworkTank.LocalPlayer.TankId : CurrentSelectedTankId;
    public static NetworkShop Singleton;
    public static event Action<float, float> OnPriceMultiplierChanged;

    public static event Action OnLocalPlayerSelectedTankChanged;

    public float tankModificationDuration;
    [FormerlySerializedAs("purchaseEffectId")]
    public string purchaseEffectInstanceId;
    [FormerlySerializedAs("purchaseEffectAssetId")]
    public string purchaseEffectId;
    public TankOffer[] tankOffers;
    [SyncVar(hook=nameof(HookPriceMultiplier))]
    private float priceMultiplier = 1;

    private readonly Dictionary<int, PendingTankUpgrade> pendingUpgrades = new();

    public readonly SyncDictionary<int, string> selectedPlayerTanks = new();

    public bool LocalPlayerCanUpgrade { get; private set; }
    public float LocalPlayerUpgradePrice { get; private set; }

    public float PriceMultiplier
    {
        get => priceMultiplier;
        set
        {
            var old = priceMultiplier;
            priceMultiplier = value;
            if (!isClient)
                HookPriceMultiplier(old, priceMultiplier);
        }
    }

    private void HookPriceMultiplier(float old, float current) => OnPriceMultiplierChanged?.Invoke(old, current);

    public NetworkShop()
    {
        Singleton = this;
    }

    private void Awake()
    {
        Singleton = this;
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<BuyTankRequest>(ServerReceiveBuyTankRequest);
    }

    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<BuyTankRequest>();
    }

    public override void OnStartClient()
    {
        selectedPlayerTanks.OnChange += OnPlrTankChange;
    }

    public override void OnStopClient()
    {
        selectedPlayerTanks.OnChange -= OnPlrTankChange;
    }

    private void OnPlrTankChange(SyncIDictionary<int, string>.Operation operation, int key, string tank)
    {
        if (key == GameNetworkAuthenticator.LocalPlayerConnId)
            OnLocalPlayerSelectedTankChanged?.Invoke();
    }

    private void Update()
    {
        if (isServer)
            ServerUpdateUpgrades();

        if (isClient)
            ClientUpdateUpgradeState();
    }

    [Server]
    private void ServerUpdateUpgrades()
    {
        if (pendingUpgrades.Count == 0)
            return;

        var time = NetworkTime.time;
        var removeList = new List<int>();

        foreach (var kv in pendingUpgrades)
        {
            var data = kv.Value;

            if (!NetworkServer.connections.TryGetValue(data.connId, out var conn) || conn == null || !conn.identity)
            {
                removeList.Add(kv.Key);
                continue;
            }

            var tank = conn.identity.GetComponent<NetworkTank>();

            if (!tank || tank.netId != data.tankNetId)
            {
                removeList.Add(kv.Key);
                continue;
            }

            if (!tank.StatusEffects.ContainsEffect(purchaseEffectInstanceId))
            {
                removeList.Add(kv.Key);
                continue;
            }

            if (tank.Death.IsDead)
            {
                removeList.Add(kv.Key);
                continue;
            }

            if (time < data.finishTime)
                continue;

            var offer = data.offer;

            if (tank.Parameters.Gears < offer.price)
            {
                removeList.Add(kv.Key);
                continue;
            }

            if (conn.identity.TryGetComponent(out NetworkTank oldTank) && !oldTank.Death.IsDead && oldTank.Parameters.Gears >= offer.price)
            {
                oldTank.Parameters.ServerRemoveGears(offer.price);
                GameTanksManager.Singleton.ServerReplacePlayerTank(conn, offer.tankId);
            }

            removeList.Add(kv.Key);
        }

        foreach (var id in removeList)
            pendingUpgrades.Remove(id);
    }

    private void ClientUpdateUpgradeState()
    {
        var upgradeIndex = TryFindLocalPlayerUpgrade();
        var playerTank = NetworkTank.LocalPlayer;

        if (upgradeIndex < 0 || !playerTank)
        {
            LocalPlayerCanUpgrade = false;
            LocalPlayerUpgradePrice = 1;
        }
        else
        {
            var offer = tankOffers[upgradeIndex];
            var isPriceOk = offer.price <= playerTank.Parameters.Gears;

            LocalPlayerCanUpgrade = isPriceOk;
            LocalPlayerUpgradePrice = offer.price;
        }
    }

    private int TryFindLocalPlayerUpgrade()
    {
        var playerTank = NetworkTank.LocalPlayer;
        if (!playerTank)
            return -1;
        for (int i = 0; i < tankOffers.Length; i++)
        {
            var tankOffer = tankOffers[i];
            if (tankOffer.requireTankId == playerTank.TankId && tankOffer.tankId != playerTank.TankId)
                return i;
        }
        return -1;
    }

    [Server]
    private void ServerReceiveBuyTankRequest(NetworkConnectionToClient conn, BuyTankRequest request)
    {
        if (!conn.isAuthenticated)
            return;

        if (request.OfferIndex < 0 || request.OfferIndex >= tankOffers.Length)
            return;

        var offer = tankOffers[request.OfferIndex];

        if (offer.requireRespawn)
        {
            if (conn.identity.TryGetComponent(out NetworkTank oldPlrTank) && oldPlrTank.TankId != offer.tankId && !oldPlrTank.Death.IsDead)
                oldPlrTank.Death.ServerKillTank(preventAllyLootPickup: true);
            GameTanksManager.Singleton.ServerSetPlayerSavedTank(conn, offer.tankId);
            return;
        }

        if (!conn.identity)
            return;

        if (!conn.identity.TryGetComponent(out NetworkTank tank))
            return;

        if (tank.StatusEffects.ContainsEffect(purchaseEffectInstanceId))
            return;

        if (tank.TankId == offer.tankId || (offer.requireTankId != "" && offer.requireTankId != tank.TankId))
            return;

        if (offer.price > tank.Parameters.Gears)
            return;

        tank.StatusEffects.ServerApplyStatusEffect(purchaseEffectInstanceId, purchaseEffectId, fromConnId: null);

        pendingUpgrades[conn.connectionId] = new PendingTankUpgrade
        {
            connId = conn.connectionId,
            offer = offer,
            finishTime = NetworkTime.time + tankModificationDuration,
            tankNetId = tank.netId
        };
    }

    [Client]
    public void ClientTryUpgrade()
    {
        var offerIndex = TryFindLocalPlayerUpgrade();
        if (offerIndex >= 0)
            ClientBuyTank(offerIndex);
    }

    [Client]
    public void ClientBuyTank(int offerIndex)
    {
        NetworkClient.Send(new BuyTankRequest { OfferIndex = offerIndex });
    }

    public struct BuyTankRequest : NetworkMessage
    {
        public int OfferIndex;
    }

    private struct PendingTankUpgrade
    {
        public int connId;
        public TankOffer offer;
        public double finishTime;
        public uint tankNetId;
    }

    [Serializable]
    public struct TankOffer
    {
        public string tankId;
        [FormerlySerializedAs("price")]
        public ushort basePrice;
        public bool requireRespawn;
        [HideInInspector]
        public ushort price
        {
            get => basePrice.Mul(Singleton ? Singleton.priceMultiplier : 1);
        }

        [Space]

        public bool isUpgrade;
        public bool isMax;
        public string requireTankId;
        public string[] banTankIds;

        [Space]

        public Sprite icon;
        public LocalizedString labelText;
    }
}
