using UnityEngine;
public class LocalPlayerUI : MonoBehaviour
{

    [Header("Tank UI")]
    [SerializeField]
    private GameObject container;
    [SerializeField]
    private TankHealthUI healthUI;
    [SerializeField]
    private TankReloadUI reloadUI;
    [SerializeField]
    private TankGearsUI gearsUI;
    [SerializeField]
    private TankShieldUI shieldUI;

    private void OnEnable()
    {
        NetworkTank.OnLocalPlayerSpawn += OnLocalPlayerSpawn;
        if (NetworkTank.LocalPlayer && !NetworkTank.LocalPlayer.Death.IsDead)
            OnLocalPlayerSpawn(NetworkTank.LocalPlayer);
    }

    private void OnDisable()
    {
        NetworkTank.OnLocalPlayerSpawn -= OnLocalPlayerSpawn;
    }

    private void OnLocalPlayerSpawn(NetworkTank localPlayerTank)
    {
        if (container)
            container.SetActive(true);
        if (healthUI)
            healthUI.SetTank(localPlayerTank);
        if (gearsUI)
            gearsUI.SetTank(localPlayerTank);
        if (reloadUI)
            reloadUI.tank = localPlayerTank;
        if (shieldUI)
            shieldUI.SetTank(localPlayerTank);
    }
}
