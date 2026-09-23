using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public class ShopMenuVisibilityController : MonoBehaviour
{
    public static bool IsVisible { get; private set; } = false;

    [SerializeField]
    private GameObject mainShopUI;
    [SerializeField]
    private float updateInterval;

    private float _timeUntilUpdate;

    private bool _shopAvailable;

    private void OnEnable()
    {
        IsVisible = mainShopUI.activeSelf;
    }

    private void OnDisable()
    {
        IsVisible = false;
    }

    private void Update()
    {
        if (_shopAvailable && !PauseMenuSwitch.PauseActive && !ChatInputUI.IsInputVisible && !LobbyMenuUI.IsMenuVisible && Keyboard.current.eKey.wasPressedThisFrame)
            SwitchShopMenuVisible();
    }

    private void LateUpdate()
    {
        _timeUntilUpdate -= Time.deltaTime;
        if (_timeUntilUpdate > 0)
            return;
        _timeUntilUpdate = updateInterval;

        _shopAvailable = !NetworkTank.LocalPlayer || NetworkTank.LocalPlayer.Death.IsDead
            || !NetworkTank.LocalPlayer.StatusEffects.ContainsEffect(NetworkShop.Singleton.purchaseEffectInstanceId);

        if (!_shopAvailable && mainShopUI.activeSelf)
        {
            mainShopUI.SetActive(false);
            IsVisible = false;
        }
    }

    private void SwitchShopMenuVisible()
    {
        var visible = !mainShopUI.activeSelf;
        mainShopUI.SetActive(visible);
        IsVisible = visible;
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ForceHide()
    {
        var changed = IsVisible != false;
        mainShopUI.SetActive(false);
        IsVisible = false;
        if (changed)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void ForceShow()
    {
        var changed = IsVisible != true;
        mainShopUI.SetActive(true);
        IsVisible = true;
        if (changed)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
