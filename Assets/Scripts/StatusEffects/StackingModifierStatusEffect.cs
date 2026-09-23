using UnityEngine;
using System.Globalization;

public class StackingModifierStatusEffect : StatusEffect
{
    public enum DurationStackMode
    {
        RefreshToFull,
        AddDuration,
        KeepLongerRemaining,
        Ignore
    }

    public enum StackExpireMode
    {
        RemoveAllAtOnce,
        RemoveOneByOne
    }

    [Header("Base")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private int maxStacks = 5;
    [SerializeField] private bool showDurationText = false;
    [SerializeField] private bool useTotalDuration = true;
    [SerializeField] private bool showDurationDecimal = false;
    [SerializeField] private bool showStacksText = true;

    [Header("First Stack Modifiers")]
    [SerializeField] private float firstStackSpeedMultiplier;
    [SerializeField] private float firstStackDamageMultiplier;
    [SerializeField] private float firstStackReloadMultiplier;

    [Header("Extra Stack Modifiers")]
    [SerializeField] private float speedMultiplierPerExtraStack;
    [SerializeField] private float damageMultiplierPerExtraStack;
    [SerializeField] private float reloadMultiplierPerExtraStack;

    [Header("Stack Behaviour")]
    [SerializeField] private DurationStackMode durationStackMode = DurationStackMode.RefreshToFull;
    [SerializeField] private StackExpireMode stackExpireMode = StackExpireMode.RemoveAllAtOnce;

    private int _stacks = 1;
    private double _expireAt;

    public int Stacks => _stacks;
    public double ExpireAt => _expireAt;

    private float _uiTimer;
    private const float UiInterval = 0.05f; // faster UI refresh for decimals

    private bool _serverRemoveScheduled;

    #region Server

    public override string ServerInit(int? fromConnId)
    {
        base.ServerInit(fromConnId);

        _stacks = 1;
        _expireAt = EffectTarget.GetTime() + duration;

        ApplyFirstStackStats(+1);

        _serverRemoveScheduled = true;
        return PackData();
    }

    public override string ServerReapply()
    {
        int oldStacks = _stacks;
        _stacks = Mathf.Clamp(_stacks + 1, 1, maxStacks);

        int addedStacks = _stacks - oldStacks;
        if (addedStacks > 0)
            ApplyExtraStackStats(addedStacks);

        ApplyDurationOnReapply();

        return PackData();
    }

    public override void ServerOnRemove()
    {
        if (_stacks >= 1)
            ApplyFirstStackStats(-1);

        if (_stacks > 1)
            ApplyExtraStackStats(-(_stacks - 1));
    }

    #endregion

    #region Client

    public override void ClientInit(string effectDataJson)
    {
        base.ClientInit(effectDataJson);
        UnpackData(effectDataJson);
    }

    public override void ClientSetEffectData(string effectDataJson)
    {
        UnpackData(effectDataJson);
    }

    public override void ClientOnRemove()
    {
        _serverRemoveScheduled = false;
    }

    #endregion

    #region Update

    protected override void TheUpdate()
    {
        if (EffectTarget == null || EffectTarget.GetTime == null)
            return;

        double now = EffectTarget.GetTime();

        UpdateText(now);

        if (_serverRemoveScheduled && now >= _expireAt)
            HandleExpire();
    }

    private void HandleExpire()
    {
        if (stackExpireMode == StackExpireMode.RemoveAllAtOnce)
        {
            _serverRemoveScheduled = false;
            EffectTarget.ServerRemoveEffect();
            return;
        }

        // RemoveOneByOne
        if (_stacks > 1)
        {
            ApplyExtraStackStats(-1);
        }
        else
        {
            ApplyFirstStackStats(-1);
        }

        _stacks--;

        if (_stacks <= 0)
        {
            _serverRemoveScheduled = false;
            EffectTarget.ServerRemoveEffect();
            return;
        }

        _expireAt = EffectTarget.GetTime() + duration;
        EffectTarget.ServerSendEffectData?.Invoke(PackData());
    }

    #endregion

    #region Logic

    private void ApplyDurationOnReapply()
    {
        double now = EffectTarget.GetTime();
        double remain = _expireAt - now;

        switch (durationStackMode)
        {
            case DurationStackMode.RefreshToFull:
                _expireAt = now + duration;
                break;

            case DurationStackMode.AddDuration:
                _expireAt += duration;
                break;

            case DurationStackMode.KeepLongerRemaining:
                _expireAt = now + Mathf.Max((float)remain, duration);
                break;

            case DurationStackMode.Ignore:
                break;
        }
    }

    private void ApplyFirstStackStats(int delta)
    {
        EffectTarget.Tank.Parameters.SpeedMultiplier += firstStackSpeedMultiplier * delta;
        EffectTarget.Tank.Parameters.DamageMultiplier += firstStackDamageMultiplier * delta;
        EffectTarget.Tank.Parameters.ReloadMultiplier += firstStackReloadMultiplier * delta;
    }

    private void ApplyExtraStackStats(int delta)
    {
        EffectTarget.Tank.Parameters.SpeedMultiplier += speedMultiplierPerExtraStack * delta;
        EffectTarget.Tank.Parameters.DamageMultiplier += damageMultiplierPerExtraStack * delta;
        EffectTarget.Tank.Parameters.ReloadMultiplier += reloadMultiplierPerExtraStack * delta;
    }

    #endregion

    #region UI

    private void UpdateText(double now)
    {
        if (!showDurationText && !showStacksText)
            return;

        _uiTimer += Time.deltaTime;
        if (_uiTimer < UiInterval)
            return;

        _uiTimer = 0f;

        string text = "";

        if (showStacksText)
            text += $"x{_stacks}";

        if (showDurationText && duration > 0)
        {
            float remain = Mathf.Max(0f, (float)(_expireAt - now));
            if (useTotalDuration)
            {
                if (stackExpireMode == StackExpireMode.RemoveOneByOne &&
                    durationStackMode != DurationStackMode.AddDuration)
                {
                    remain += Mathf.Max(0, _stacks - 1) * duration;
                }
            }

            if (text != "")
                text += " ";

            if (showDurationDecimal)
                text += remain.ToString("0.0", CultureInfo.InvariantCulture) + "s";
            else
                text += Mathf.CeilToInt(remain) + "s";
        }

        EffectTarget.SetDisplayText(text);
    }

    #endregion

    #region Net Data

    private string PackData()
    {
        return _stacks + "|" + _expireAt.ToString(CultureInfo.InvariantCulture);
    }

    private void UnpackData(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return;

        string[] parts = data.Split('|');
        if (parts.Length < 2)
            return;

        int.TryParse(parts[0], out _stacks);
        parts[1].TryParseNoLocale(out _expireAt);
    }

    #endregion
}