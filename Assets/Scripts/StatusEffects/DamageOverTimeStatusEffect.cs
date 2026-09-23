using System;
using System.Linq;
using UnityEngine;

public class DamageOverTimeStatusEffect : StatusEffect
{
    [Header("Status Effect Parameters")]
    [SerializeField] private ushort damagePerTick;
    [SerializeField] private ushort applyTicks;
    [SerializeField] private ushort ticksLimit;
    [SerializeField] private float tickInterval;

    [Header("Icon and Text")]
    [SerializeField] private bool showTicksCountText;
    [SerializeField] private DamageOverTimeIcon[] icons;

    [Header("FX")]
    [SerializeField] private GameObject damageTickFXPrefab;

    private EffectData _data;

    // --- runtime helpers ---
    private double _nextServerTickTime;
    private double _lastVisualTickTimestamp;

    public override string ServerInit(int? fromConnId)
    {
        base.ServerInit(fromConnId);

        _data = new EffectData
        {
            damagePerTick = damagePerTick,
            tickInterval = tickInterval,
            ticksLeft = applyTicks,
            lastTickTimestamp = EffectTarget.GetTime()
        };

        _nextServerTickTime = _data.lastTickTimestamp + _data.tickInterval;
        _lastVisualTickTimestamp = _data.lastTickTimestamp;

        UpdateDisplayInfo();

        return JsonUtility.ToJson(_data);
    }

    public override string ServerReapply()
    {
        _data.ticksLeft = _data.ticksLeft.Add(applyTicks).ClampToMax(ticksLimit);
        UpdateDisplayInfo();
        return JsonUtility.ToJson(_data);
    }

    public override void ServerOnRemove()
    {
        _data.ticksLeft = 0;
    }

    public override void ClientInit(string effectDataJson)
    {
        base.ClientInit(effectDataJson);

        var dataObject = JsonUtility.FromJson(effectDataJson, typeof(EffectData));
        if (dataObject is EffectData effectData)
        {
            _data = effectData;
            _lastVisualTickTimestamp = _data.lastTickTimestamp;
        }

        UpdateDisplayInfo();
    }

    public override void ClientSetEffectData(string effectDataJson)
    {
        var dataObject = JsonUtility.FromJson(effectDataJson, typeof(EffectData));
        if (dataObject is EffectData effectData)
            _data = effectData;

        UpdateDisplayInfo();
    }

    public override void ClientOnRemove()
    {
        if (_data.ticksLeft != 0)
        {
            if (damageTickFXPrefab)
                Instantiate(damageTickFXPrefab, transform.position, transform.rotation);

            _data.lastTickTimestamp = EffectTarget.GetTime();
        }

        _data.ticksLeft = 0;
    }

    protected override void TheUpdate()
    {
        if (_data == null)
            return;

        if (EffectTarget.Tank.isServer)
            ServerUpdate();

        if (EffectTarget.Tank.isClient)
            ClientVisualUpdate();
    }

    private void ServerUpdate()
    {
        if (_data.ticksLeft == 0)
            return;

        double time = EffectTarget.GetTime();

        if (time >= _nextServerTickTime)
        {
            EffectTarget.Tank.Parameters.ServerDealDamage(
                _data.damagePerTick,
                fromPlayerConnId: ServerFromConnId);

            _data.ticksLeft -= 1;

            _data.lastTickTimestamp = time;
            _nextServerTickTime = time + _data.tickInterval;

            if (_data.ticksLeft > 0)
            {
                EffectTarget.ServerSendEffectData(JsonUtility.ToJson(_data));
            }
            else
            {
                EffectTarget.ServerRemoveEffect();
            }
        }
    }

    private void ClientVisualUpdate()
    {
        if (_data.ticksLeft == 0)
            return;

        if (Math.Abs(_lastVisualTickTimestamp - _data.lastTickTimestamp) > 0.01)
        {
            _lastVisualTickTimestamp = _data.lastTickTimestamp;

            UpdateDisplayInfo();

            if (damageTickFXPrefab)
                Instantiate(damageTickFXPrefab, transform.position, transform.rotation);
        }
    }

    private void UpdateDisplayInfo()
    {
        if (icons.Length > 0)
        {
            var dotIcon = icons
                .OrderByDescending(dotIcon => dotIcon.minTicksCount)
                .FirstOrDefault(dotIcon => dotIcon.minTicksCount <= _data.ticksLeft);

            if (dotIcon != null)
            {
                EffectTarget.SetDisplayIcon(dotIcon.icon);
                EffectTarget.SetDisplayIconColor(dotIcon.color);
            }
        }

        if (showTicksCountText)
            EffectTarget.SetDisplayText("x" + _data.ticksLeft);
    }

    [Serializable]
    public class EffectData
    {
        public double lastTickTimestamp;
        public ushort damagePerTick;
        public float tickInterval;
        public ushort ticksLeft;
    }

    [Serializable]
    public class DamageOverTimeIcon
    {
        public Sprite icon;
        public Color color;
        public int minTicksCount;
    }
}