using System;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
public class TankShieldDisplay : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Shield Display")]
    [SerializeField]
    private GameObject shieldDisplayContainer;
    [SerializeField]
    private TextMeshPro shieldDisplayText;

    private void Start()
    {
        UpdateShieldDisplay();
    }

    private void OnEnable()
    {
        tank.Parameters.shields.OnChange += OnShieldChanged;
        UpdateShieldDisplay();
    }

    private void OnDisable()
    {
        tank.Parameters.shields.OnChange -= OnShieldChanged;
    }

    private void OnShieldChanged(SyncIDictionary<string, ushort>.Operation operation, string arg2, ushort arg3)
    {
        UpdateShieldDisplay();
    }

    private void UpdateShieldDisplay()
    {
        var overalShieldValue = tank.Parameters.shields.Values.Sum((shieldValue) => shieldValue);
        shieldDisplayContainer.SetActive(overalShieldValue > 0);
        shieldDisplayText.text = overalShieldValue.ToString();
    }
}
