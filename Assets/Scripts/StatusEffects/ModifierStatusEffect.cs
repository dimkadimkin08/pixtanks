using UnityEngine;

public class ModifierStatusEffect : StatusEffect
{
    [Header("Status Effect Parameters")]
    [SerializeField] private float duration;
    [SerializeField] private float speedMultiplierModifier;
    [SerializeField] private float damageMultiplierModifier;
    [SerializeField] private float reloadMultiplierModifier;

    [Space]
    [SerializeField] private bool showDurationText;

    private double _createdAt;

    private float _durationTextTimer;
    private const float DurationTextInterval = 0.26f;

    private bool _serverRemoveScheduled;

    public override string ServerInit(int? fromConnId)
    {
        base.ServerInit(fromConnId);

        _createdAt = EffectTarget.GetTime();

        EffectTarget.Tank.Parameters.SpeedMultiplier += speedMultiplierModifier;
        EffectTarget.Tank.Parameters.DamageMultiplier += damageMultiplierModifier;
        EffectTarget.Tank.Parameters.ReloadMultiplier += reloadMultiplierModifier;

        _serverRemoveScheduled = true;

        if (showDurationText && duration > 0)
        {
            _durationTextTimer = 0f;
        }

        return _createdAt.ToString();
    }

    public override string ServerReapply()
    {
        _createdAt = EffectTarget.GetTime();
        return _createdAt.ToString();
    }

    public override void ServerOnRemove()
    {
        EffectTarget.Tank.Parameters.SpeedMultiplier -= speedMultiplierModifier;
        EffectTarget.Tank.Parameters.DamageMultiplier -= damageMultiplierModifier;
        EffectTarget.Tank.Parameters.ReloadMultiplier -= reloadMultiplierModifier;
    }

    public override void ClientInit(string effectDataJson)
    {
        base.ClientInit(effectDataJson);

        effectDataJson.TryParseNoLocale(out _createdAt);

        if (showDurationText && duration > 0)
        {
            _durationTextTimer = 0f;
        }
    }

    public override void ClientSetEffectData(string effectDataJson)
    {
        effectDataJson.TryParseNoLocale(out _createdAt);
    }

    public override void ClientOnRemove()
    {
        _serverRemoveScheduled = false;
    }

    protected override void TheUpdate()
    {
        double currentTime = EffectTarget.GetTime();

        if (showDurationText && duration > 0)
        {
            _durationTextTimer += Time.deltaTime;

            if (_durationTextTimer >= DurationTextInterval)
            {
                _durationTextTimer = 0f;

                double timeLeft = _createdAt + duration - currentTime;
                EffectTarget.SetDisplayText(
                    Mathf.RoundToInt((float)timeLeft).ToString() + "s"
                );
            }
        }

        if (_serverRemoveScheduled)
        {
            if (duration > 0 && currentTime >= _createdAt + duration)
            {
                _serverRemoveScheduled = false;
                EffectTarget.ServerRemoveEffect();
            }
        }
    }
}