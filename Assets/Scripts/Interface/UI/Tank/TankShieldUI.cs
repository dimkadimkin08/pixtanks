using System;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
public class TankShieldUI : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Shields UI")]
    [SerializeField]
    private Text shieldsValueText;

    private void Start()
    {
        if (tank)
            UpdateShields();
    }

    private void OnEnable()
    {
        if (!tank)
            return;
        tank.Parameters.shields.OnChange += OnShieldsChanged;
        UpdateShields();
    }

    private void OnDisable()
    {
        if (!tank)
            return;
        tank.Parameters.shields.OnChange -= OnShieldsChanged;
    }

    private void OnShieldsChanged(SyncIDictionary<string, ushort>.Operation operation, string arg2, ushort arg3)
    {
        UpdateShields();
    }

    private void UpdateShields()
    {
        if (!tank)
            return;
        var shieldsValue = tank.Parameters.shields.Sum(shield => shield.Value);
        if (shieldsValueText)
            shieldsValueText.text = shieldsValue.ToString();
    }

    public void SetTank(NetworkTank newTank)
    {
        if (tank)
            tank.Parameters.shields.OnChange -= OnShieldsChanged;
        tank = newTank;
        tank.Parameters.shields.OnChange += OnShieldsChanged;
        UpdateShields();
    }
}
