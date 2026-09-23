using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
public class PlayerTankUpgradeDisplay : MonoBehaviour
{
    [SerializeField]
    private GameObject upgradeDisplayContainer;
    [SerializeField]
    private TextMeshPro priceText;

    private bool _wasAbleToUpgrade;

    private void OnEnable()
    {
        if (upgradeDisplayContainer)
            upgradeDisplayContainer.SetActive(_wasAbleToUpgrade);
    }

    private void Update()
    {
        var ableToUpgrade = NetworkShop.Singleton && NetworkShop.Singleton.LocalPlayerCanUpgrade;

        if (upgradeDisplayContainer && ableToUpgrade != _wasAbleToUpgrade)
        {
            _wasAbleToUpgrade = ableToUpgrade;
            upgradeDisplayContainer.SetActive(ableToUpgrade);
        }

        if (upgradeDisplayContainer && ableToUpgrade && NetworkTank.LocalPlayer)
            upgradeDisplayContainer.transform.position = NetworkTank.LocalPlayer.transform.position;

        if (priceText && ableToUpgrade)
            priceText.text = NetworkShop.Singleton ? NetworkShop.Singleton.LocalPlayerUpgradePrice.ToString() : "0";

        if (ableToUpgrade && Keyboard.current.xKey.wasPressedThisFrame)
        {
            NetworkShop.Singleton.ClientTryUpgrade();
        }
    }
}