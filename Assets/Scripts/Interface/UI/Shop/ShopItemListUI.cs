using Mirror;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static NetworkShop;

public class ShopItemListUI : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private ShopMenuVisibilityController menuVisibilityController;

    [Header("Layouts")]
    [SerializeField] private List<TankOffersContainer> offerContainers = new();

    [Header("Prefab")]
    [SerializeField] private ShopItemUI tankItemPrefab;

    private uint _lastLocalPlayerId;

    private void Start()
    {
        if (!AppPlatform.IsHeadless)
            Refresh();
        UpdateLayoutSizes();
    }

    private void OnEnable()
    {
        NetworkTank.OnLocalPlayerSpawn += OnLocalPlayerSpawn;
        NetworkTank.OnLocalPlayerDead += OnLocalPlayerDead;
        NetworkShop.OnPriceMultiplierChanged += OnPriceMultiplierChanged;
        NetworkShop.OnLocalPlayerSelectedTankChanged += OnLocalPlayerSelectedTankChanged;
        if (!AppPlatform.IsHeadless)
            Refresh();
    }

    private void OnDisable()
    {
        NetworkTank.OnLocalPlayerSpawn -= OnLocalPlayerSpawn;
        NetworkTank.OnLocalPlayerDead -= OnLocalPlayerDead;
        NetworkShop.OnPriceMultiplierChanged -= OnPriceMultiplierChanged;
        NetworkShop.OnLocalPlayerSelectedTankChanged -= OnLocalPlayerSelectedTankChanged;
    }

    private void Update()
    {
        if (ShopMenuVisibilityController.IsVisible)
            UpdateLayoutSizes();
        TryDetectLocalPlayerChange();
    }

    private void OnLocalPlayerSelectedTankChanged()
    {
        Refresh();
    }

    private void OnLocalPlayerDead()
    {
        Refresh();
    }

    private void UpdateLayoutSizes()
    {
        foreach (var container in offerContainers)
        {
            container.listContentLayout.cellSize = new Vector2(
                container.listContentRect.rect.size.x / container.shopItemsInRow,
                container.listContentRect.rect.size.y / container.shopRowsCount);
        }
    }

    private void TryDetectLocalPlayerChange()
    {
        var localPlayer = NetworkTank.LocalPlayer;
        if (localPlayer && _lastLocalPlayerId != localPlayer.netId)
            OnLocalPlayerSpawn(localPlayer);
    }

    private void OnPriceMultiplierChanged(float _, float __)
    {
        if (!AppPlatform.IsHeadless)
            Refresh();
    }

    private void OnLocalPlayerSpawn(NetworkTank tank)
    {
        _lastLocalPlayerId = tank.netId;

        if (!AppPlatform.IsHeadless)
            Refresh();
    }

    private void Refresh()
    {
        foreach (var container in offerContainers)
            ClearLayout(container.listContentRect);

        var tankOffers = NetworkShop.Singleton.tankOffers;

        var currentTankId = NetworkShop.CurrentTankId;

        var currentOffer = tankOffers.FirstOrDefault(t => t.tankId == currentTankId);
        var isCurrentTankMax = currentOffer.isMax;

        var upgradeOffer = tankOffers.FirstOrDefault(t =>
            (t.isUpgrade) && t.requireTankId == currentTankId);

        var filteredOffers = tankOffers
            .Select((offer, index) => new { offer, index })
            .Where(x => IsOfferVisible(x.offer, currentTankId))
            //.OrderBy(x => x.offer.isUpgrade || x.offer.isMax ? 0 : 1)
            .ToArray();

        var teamColor = NetworkTank.LocalPlayer && GameTanksManager.Singleton
            ? GameTanksManager.Singleton.GetTeam(NetworkTank.LocalPlayer.Parameters.TeamId).color
            : Color.white;
        var containerIndex = 0;
        var offersInContainer = 0;
        for (int i = 0; i < filteredOffers.Length; i++)
        {
            offersInContainer++;
            if (offerContainers[containerIndex].itemLimit < offersInContainer && offerContainers.Count > 1 + containerIndex)
            {
                containerIndex++;
                offersInContainer = 1;
            }
            CreateItem(offerContainers[containerIndex], filteredOffers[i].offer, filteredOffers[i].index,
                currentTankId, isCurrentTankMax, upgradeOffer, teamColor);
        }
    }

    private void ClearLayout(RectTransform root)
    {
        foreach (Transform child in root)
            Destroy(child.gameObject);
    }

    private bool IsOfferVisible(TankOffer offer, string currentTankId)
    {
        if (!string.IsNullOrEmpty(offer.requireTankId) &&
            offer.requireTankId != currentTankId &&
            (!offer.isMax || offer.tankId != currentTankId))
            return false;

        if (!offer.isMax && offer.tankId == currentTankId)
            return false;

        if (offer.banTankIds.Contains(currentTankId))
            return false;

        return true;
    }

    private void CreateItem(
        TankOffersContainer container,
        TankOffer offer,
        int index,
        string currentTankId,
        bool isCurrentTankMax,
        TankOffer upgradeOffer,
        Color teamColor)
    {
        bool isUpgrade = offer.tankId == upgradeOffer.tankId;
        bool isMax = isCurrentTankMax && offer.tankId == currentTankId;

        var parent = container.listContentRect;

        var item = Instantiate(tankItemPrefab, parent);

        offer.labelText.LoadWithCallback(label =>
        {
            if (item)
            {
                item.Init(
                    itemId: offer.tankId,
                    type: GetItemType(isMax, isUpgrade),
                    icon: offer.icon,
                    label: label,
                    requireRespawn: offer.requireRespawn,
                    price: offer.price,
                    onClick: () => TryBuy(offer, index)
                );
                item.SetTeamColor(teamColor);
            }
        });
    }

    private ShopItemUI.ItemType GetItemType(bool isMax, bool isUpgrade)
    {
        if (isMax) return ShopItemUI.ItemType.Max;
        if (isUpgrade) return ShopItemUI.ItemType.Upgrade;
        return ShopItemUI.ItemType.Buy;
    }

    private void TryBuy(TankOffer offer, int index)
    {
        if (!NetworkClient.active)
            return;

        if (!CanBuy(offer, NetworkTank.LocalPlayer))
            return;

        NetworkShop.Singleton.ClientBuyTank(index);
        menuVisibilityController.ForceHide();
    }

    private bool CanBuy(TankOffer offer, NetworkTank player)
    {
        if (offer.requireRespawn)
            return true;

        if (offer.tankId == NetworkShop.CurrentTankId)
            return false;

        if (!string.IsNullOrEmpty(offer.requireTankId) &&
            offer.requireTankId != player.TankId)
            return false;

        if (offer.price > player.Parameters.Gears &&
            player.Parameters.CurrentHp < player.Parameters.MaxHp)
            return false;

        return true;
    }

    [Serializable]
    public class TankOffersContainer
    {
        public RectTransform listContentRect;
        public GridLayoutGroup listContentLayout;
        public uint shopRowsCount = 2;
        public uint shopItemsInRow = 2;
        public ushort itemLimit = 4;
    }
}