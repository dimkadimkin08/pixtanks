using System;
using System.Collections;
using System.Globalization;
using Mirror;
using UnityEngine;

public class ShieldStatusEffect : StatusEffect
{
    [Header("Status Effect Parameters")]
    [SerializeField] private float duration;
    [SerializeField] private string shieldId;
    [SerializeField] private ushort baseShieldLimit;
    [SerializeField] private float shieldLimitMaxHpRatio;
    [SerializeField, Range(0, 1)] private float shieldGain;

    [Header("Shield State Display")]
    [SerializeField] private GameObject fullShieldIndicator;
    [SerializeField] private SpriteRenderer shieldSprite;
    [SerializeField] private Gradient shieldAmountToColor;

    [Header("Duration Warning FX")]
    [SerializeField] private GameObject durationWarningFXChild;
    [SerializeField] private float warningDelay;

    [Header("Damage FX")]
    [SerializeField] private GameObject damageEffectChild;
    [SerializeField] private float damageEffectDuration;
    [SerializeField] private AudioSource damageSound;

    [Header("Destroy FX")]
    [SerializeField] private GameObject destroyFXIndependentChild;

    private ushort ShieldLimit => (ushort)(baseShieldLimit + Mathf.RoundToInt(shieldLimitMaxHpRatio * EffectTarget.Tank.Parameters.MaxHp));
    private ushort ShieldGain => ShieldLimit.Mul(shieldGain);

    private double _createdAt;
    private ushort _lastShieldValue;

    // --- timers / states ---
    private bool _serverRemoveScheduled;

    private bool _warningActive;
    private bool _warningInitialized;

    private float _damageEffectTimer;
    private bool _damageEffectPlaying;

    public override string ServerInit(int? fromConnId)
    {
        base.ServerInit(fromConnId);

        _createdAt = EffectTarget.GetTime();

        if (!EffectTarget.Tank.Parameters.shields.TryGetValue(shieldId, out var oldShieldAmount))
            oldShieldAmount = 0;

        EffectTarget.Tank.Parameters.shields[shieldId] =
            oldShieldAmount.Add(ShieldGain).ClampToMax(ShieldLimit);

        EffectTarget.Tank.Parameters.shields.OnChange += ServerOnShieldsChange;

        if (EffectTarget.Tank.Parameters.shields.TryGetValue(shieldId, out var currentShieldValue))
            _lastShieldValue = currentShieldValue;

        _serverRemoveScheduled = true;
        _warningInitialized = false;

        return _createdAt.ToString(CultureInfo.CreateSpecificCulture("en-US"));
    }

    public override string ServerReapply()
    {
        _createdAt = EffectTarget.GetTime();

        if (!EffectTarget.Tank.Parameters.shields.TryGetValue(shieldId, out var oldShieldAmount))
            oldShieldAmount = 0;

        if (EffectTarget.Tank.Parameters.shields.TryGetValue(shieldId, out var currentShieldValue))
            _lastShieldValue = currentShieldValue;

        EffectTarget.Tank.Parameters.shields[shieldId] =
            oldShieldAmount.Add(ShieldGain).ClampToMax(ShieldLimit);

        return _createdAt.ToString(CultureInfo.CreateSpecificCulture("en-US"));
    }

    public override void ServerOnRemove()
    {
        EffectTarget.Tank.Parameters.shields.OnChange -= ServerOnShieldsChange;
        EffectTarget.Tank.Parameters.shields.Remove(shieldId);

        if (destroyFXIndependentChild)
        {
            destroyFXIndependentChild.transform.parent = null;
            destroyFXIndependentChild.SetActive(true);
        }
    }

    public override void ClientInit(string effectDataJson)
    {
        base.ClientInit(effectDataJson);

        effectDataJson.TryParseNoLocale(out _createdAt);

        if (EffectTarget.Tank.Parameters.shields.TryGetValue(shieldId, out var currentShieldValue))
            _lastShieldValue = currentShieldValue;

        EffectTarget.Tank.Parameters.shields.OnChange += ClientOnShieldsChange;

        _warningInitialized = false;
    }

    public override void ClientSetEffectData(string effectDataJson)
    {
        effectDataJson.TryParseNoLocale(out _createdAt);

        if (EffectTarget.Tank.Parameters.shields.TryGetValue(shieldId, out var currentShieldValue))
            _lastShieldValue = currentShieldValue;
    }

    public override void ClientOnRemove()
    {
        EffectTarget.Tank.Parameters.shields.OnChange -= ClientOnShieldsChange;

        if (destroyFXIndependentChild)
        {
            destroyFXIndependentChild.transform.parent = null;
            destroyFXIndependentChild.SetActive(true);
        }
    }

    protected override void TheUpdate()
    {
        double currentTime = EffectTarget.GetTime();

        // --- Shield visuals ---
        if (NetworkClient.active)
        {
            shieldSprite.color = shieldAmountToColor.Evaluate(
                Mathf.Clamp01(_lastShieldValue / (float)ShieldLimit));

            var isFullShield = _lastShieldValue >= ShieldLimit;

            if (fullShieldIndicator && fullShieldIndicator.activeSelf != isFullShield)
                fullShieldIndicator.SetActive(isFullShield);
        }

        // === ServerRemoveAfterDuration ===
        if (_serverRemoveScheduled)
        {
            if (duration > 0 && currentTime >= _createdAt + duration)
            {
                _serverRemoveScheduled = false;
                EffectTarget.ServerRemoveEffect();
            }
        }

        // === ControlWarningVisibility ===
        if (durationWarningFXChild)
        {
            if (!_warningInitialized)
            {
                durationWarningFXChild.SetActive(false);
                _warningInitialized = true;
                _warningActive = false;
            }

            if (!_warningActive)
            {
                if (warningDelay > 0 && currentTime >= _createdAt + warningDelay)
                {
                    durationWarningFXChild.SetActive(true);
                    _warningActive = true;
                }
            }
            else
            {
                if (!(warningDelay > 0 && currentTime > _createdAt + warningDelay))
                {
                    durationWarningFXChild.SetActive(false);
                    _warningActive = false;
                }
            }
        }

        // === ShowDamageEffect ===
        if (_damageEffectPlaying)
        {
            _damageEffectTimer -= Time.deltaTime;

            if (_damageEffectTimer <= 0f)
            {
                _damageEffectPlaying = false;

                if (damageEffectChild)
                    damageEffectChild.SetActive(false);
            }
        }
    }

    private void ServerOnShieldsChange(SyncIDictionary<string, ushort>.Operation operation, string arg2, ushort arg3)
    {
        if (!EffectTarget.Tank.Parameters.shields.TryGetValue(shieldId, out var currentShieldValue) || currentShieldValue == 0)
        {
            EffectTarget.ServerRemoveEffect();
        }

        if (NetworkClient.active)
        {
            ClientOnShieldsChange(operation, arg2, arg3);
        }
    }

    private void ClientOnShieldsChange(SyncIDictionary<string, ushort>.Operation operation, string arg2, ushort arg3)
    {
        if (!EffectTarget.Tank.Parameters.shields.TryGetValue(shieldId, out var currentShieldValue))
            return;

        if (_lastShieldValue > currentShieldValue)
        {
            // аналог StopCoroutine + StartCoroutine
            StartDamageEffect();

            if (damageSound && damageSound.enabled)
                damageSound.Play();
        }

        _lastShieldValue = currentShieldValue;
    }

    private void StartDamageEffect()
    {
        _damageEffectPlaying = true;
        _damageEffectTimer = damageEffectDuration;

        if (damageEffectChild)
        {
            damageEffectChild.SetActive(false);
            damageEffectChild.SetActive(true);
        }
    }
}