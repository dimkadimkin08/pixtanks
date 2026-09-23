using System;
using TMPro;
using UnityEngine;

public class TaknDebugDisplay : MonoBehaviour
{
    private static bool DebugDisplayEnabled;
    private static event Action<bool> OnDebugDisplaySwitch;

    public static bool IsDebugDisplayEnabled
    {
        get => DebugDisplayEnabled;
        set
        {
            DebugDisplayEnabled = value;
            OnDebugDisplaySwitch?.Invoke(value);
        }
    }

    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Debug Display")]
    [SerializeField]
    private TextMeshPro debugText;

    private void Start()
    {
        SetDebugDisplayEnabled(IsDebugDisplayEnabled);
    }

    private void OnEnable()
    {
        OnDebugDisplaySwitch += SetDebugDisplayEnabled;
    }

    private void OnDisable()
    {
        OnDebugDisplaySwitch -= SetDebugDisplayEnabled;
    }

    private void Update()
    {
        if (IsDebugDisplayEnabled)
            RefreshDebugText();
    }

    private void RefreshDebugText()
    {
        debugText.text = $"netId: {tank.netId}\npos: {(Vector2)transform.position}";
    }

    private void SetDebugDisplayEnabled(bool debugDisplayEnabled)
    {
        debugText.enabled = debugDisplayEnabled;
    }

}