using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
public class ShopItemUI : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private Text labelText;
    [SerializeField]
    private Text priceText;
    [SerializeField]
    private Text buyButtonText;
    [SerializeField]
    private Button buyButton;
    [SerializeField]
    private GameObject[] hideWhenMax;
    [SerializeField]
    private GameObject upgradeIndicator;
    [SerializeField]
    private GameObject upgradeAllowedIndicator;

    [Space]

    [SerializeField]
    private LocalizedString buyText;
    [SerializeField]
    private LocalizedString upgradeText;
    [SerializeField]
    private LocalizedString maxText;


    [Space]

    [SerializeField]
    private GameObject newTankUI;
    [SerializeField]
    private GameObject upgradeUI;

    private string _itemId;
    private bool _isUpgrade;
    private ushort _price;
    private bool _requireRespawn;

    public enum ItemType { Buy, Upgrade, Max }
    
    public void Init(ItemType type, Sprite icon, string itemId, string label, ushort price, bool requireRespawn, Action onClick = null)
    {
        if (!gameObject)
            return;
        switch(type)
        {
            case ItemType.Buy:
                buyText.LoadWithCallback(result => { if (buyButtonText) buyButtonText.text = result; });
                break;
            case ItemType.Upgrade:
                upgradeText.LoadWithCallback(result => { if (buyButtonText) buyButtonText.text = result; });
                break;
            case ItemType.Max:
                maxText.LoadWithCallback(result => { if (buyButtonText) buyButtonText.text = result; });
                break;
            default:
                buyButtonText.text = "";
                break;
        };
        if (newTankUI)
            newTankUI.SetActive(type is ItemType.Buy);
        if (upgradeUI)
            upgradeUI.SetActive(type is ItemType.Upgrade);
        if (upgradeIndicator)
            upgradeIndicator.SetActive(type != ItemType.Buy);
        hideWhenMax = hideWhenMax.Where(item => (bool)item).ToArray();
        foreach (var obj in hideWhenMax)
            if (obj)
                obj.SetActive(type != ItemType.Max);
        iconImage.sprite = icon;
        labelText.text = label;
        priceText.text = price.ToString();
        _itemId = itemId;
        _price = price;
        _requireRespawn = requireRespawn;
        _isUpgrade = type == ItemType.Upgrade;
        buyButton.onClick.AddListener(() => 
        {
            if (type != ItemType.Max)
                onClick?.Invoke();
        });
    }

    public void SetTeamColor(Color color)
    {
        iconImage.color = new Color(color.r, color.g, color.b, iconImage.color.a);
    }

    private void Update()
    {
        var notSameTank = !NetworkTank.LocalPlayer || NetworkShop.CurrentTankId != _itemId;
        var priceOk = NetworkTank.LocalPlayer && _price <= NetworkTank.LocalPlayer.Parameters.Gears;
        var ableToBuy = notSameTank && (priceOk || _requireRespawn);
        buyButton.interactable = ableToBuy;
        if (upgradeAllowedIndicator)
            upgradeAllowedIndicator.SetActive(_isUpgrade && ableToBuy);
    }
}