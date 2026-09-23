using System;
using System.Collections;
using System.Linq;
using UnityEngine;
public class StructureDamageOverTimeStatusEffect : StructureStatusEffect
{
    [Header("Status Effect Parameters")]
    [SerializeField]
    private ushort damagePerTick;
    [SerializeField]
    private ushort applyTicks;
    [SerializeField]
    private ushort ticksLimit;
    [SerializeField]
    private float tickInterval;

    [Header("Icon and Text")]
    [SerializeField]
    private bool showTicksCountText;
    [SerializeField]
    private DamageOverTimeIcon[] icons;

    [Header("FX")]
    [SerializeField]
    private GameObject damageTickFXPrefab;

    private EffectData _data;

    public override string ServerInit()
    {
        base.ServerInit();
        _data = new EffectData
        {
            damagePerTick = damagePerTick,
            tickInterval = tickInterval,
            ticksLeft = applyTicks,
            lastTickTimestamp = EffectTarget.GetTime()
        };
        StartCoroutine(ServerDealDamageOverTime());
        StartCoroutine(ShowDamageEffectVisuals());
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
            StartCoroutine(ShowDamageEffectVisuals());
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

    private IEnumerator ServerDealDamageOverTime()
    {
        var waitForTick = new WaitWhile(() => _data.ticksLeft != 0 && EffectTarget.GetTime() < _data.lastTickTimestamp + _data.tickInterval);
        while (_data.ticksLeft != 0)
        {
            yield return waitForTick;
            if (_data.ticksLeft == 0)
                break;
            EffectTarget.Structure.ServerDealDamage(_data.damagePerTick);
            _data.ticksLeft--;
            _data.lastTickTimestamp = EffectTarget.GetTime();
            if (_data.ticksLeft > 0)
                EffectTarget.ServerSendEffectData(JsonUtility.ToJson(_data));
        }
        EffectTarget.ServerRemoveEffect();
    }

    private void UpdateDisplayInfo()
    {
        if (icons.Length > 0)
        {
            var dotIcon = icons.OrderByDescending(dotIcon => dotIcon.minTicksCount)
                .FirstOrDefault(dotIcon => dotIcon.minTicksCount <= _data.ticksLeft);
            if (dotIcon != default)
            {
                EffectTarget.SetDisplayIcon(dotIcon.icon);
                EffectTarget.SetDisplayIconColor(dotIcon.color);
            }
        }
        if (showTicksCountText)
            EffectTarget.SetDisplayText("x" + _data.ticksLeft.ToString());
    }

    private IEnumerator ShowDamageEffectVisuals()
    {
        while (_data.ticksLeft != 0)
        {
            var lastTimestamp = _data.lastTickTimestamp;
            yield return new WaitWhile(() => Math.Abs(lastTimestamp - _data.lastTickTimestamp) < 0.01);
            UpdateDisplayInfo();
            if (damageTickFXPrefab)
                Instantiate(damageTickFXPrefab, transform.position, transform.rotation);
        }
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
