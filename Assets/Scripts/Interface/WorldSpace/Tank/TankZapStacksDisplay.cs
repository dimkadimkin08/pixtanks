using TMPro;
using UnityEngine;
public class TankZapStacksDisplay : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Zap Stacks Display")]
    [SerializeField]
    private TextMeshPro stacksDisplayText;

    private ZapTankShootingRules _zapRules;

    private void Start()
    {
        if (tank.Shoot.ShootingRules is ZapTankShootingRules zapRules)
            _zapRules = zapRules;
    }

    private void Update()
    {
        stacksDisplayText.text = _zapRules ? _zapRules.CurrentStacks.ToString() : "0";
    }
}
