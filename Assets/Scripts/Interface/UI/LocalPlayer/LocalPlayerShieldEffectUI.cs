using System;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using static GameTanksManager;
public class LocalPlayerShieldEffectUI : MonoBehaviour
{

    [Header("Local Player Shield Effect UI")]
    [SerializeField]
    private Image effectImage;
    [SerializeField]
    private float alphaDeceleration = 2;
    [SerializeField, Range(0, 1)]
    private float maxAlpha = 0.75f;

    private NetworkTank tank;

    private void OnEnable()
    {
        NetworkTank.OnLocalPlayerSpawn += OnLocalPlayerSpawn;
        NetworkTank.OnLocalPlayerDestroyed += OnLocalPlayerDestroyed;
        if (NetworkTank.LocalPlayer)
            OnLocalPlayerSpawn(NetworkTank.LocalPlayer);
    }

    private void OnDisable()
    {
        NetworkTank.OnLocalPlayerSpawn -= OnLocalPlayerSpawn;
        NetworkTank.OnLocalPlayerDestroyed -= OnLocalPlayerDestroyed;
    }

    private void Update()
    {
        if (effectImage.color.a == 0)
            return;
        float alpha = Mathf.Lerp(effectImage.color.a, 0, alphaDeceleration * Time.deltaTime);
        effectImage.color = new Color(effectImage.color.r, effectImage.color.g, effectImage.color.b, alpha);
    }

    private void OnLocalPlayerSpawn(NetworkTank localPlayerTank)
    {
        tank = localPlayerTank;
        tank.Parameters.shields.OnChange += OnShieldsChanged;
    }

    private void OnLocalPlayerDestroyed()
    {
        effectImage.color = new Color(effectImage.color.r, effectImage.color.g, effectImage.color.b, 0);
    }

    private void OnShieldsChanged(SyncIDictionary<string, ushort>.Operation operation, string arg2, ushort arg3)
    {
        if (tank && tank.Parameters.CurrentHp > 0)
         effectImage.color = new Color(effectImage.color.r, effectImage.color.g, effectImage.color.b, maxAlpha);
    }
}
