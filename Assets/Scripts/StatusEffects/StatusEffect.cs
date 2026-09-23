using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class StatusEffect : MonoBehaviour
{
    [Header("Display")]
    public Sprite icon;
    [SerializeField] protected TeamColorMode iconColorMode = TeamColorMode.Fixed;
    [FormerlySerializedAs("iconColor")]
    public Color fixedIconColor = Color.white;
    public string defaultText = "";

    [Header("Team Color Apply")]
    [SerializeField] protected List<SpriteRenderer> fromTeamSpriteRenderers = new();

    [SerializeField] protected List<TrailRenderer> fromTeamTrails = new();

    [SerializeField] protected List<ParticleSystem> fromTeamParticles = new();

    public enum TeamColorMode
    {
        Fixed,
        FromTeam
    }

    public class StatusEffectTarget
    {
        public delegate void SendDataDelegate(string dataJson);

        public NetworkTank Tank;

        public Func<double> GetTime;

        public Action<string> ServerSendEffectData;
        public Action ServerRemoveEffect;
        public Action<Sprite> SetDisplayIcon;
        public Action<Color> SetDisplayIconColor;
        public Action<string> SetDisplayText;
    }

    public StatusEffectTarget EffectTarget;

    protected int? ServerFromConnId { get; private set; } = null;

    protected string FromTeamId { get; private set; }

    #region Init

    public void SetFromTeamId(string fromTeamId)
    {
        FromTeamId = fromTeamId;
        ApplyTeamColors();
    }

    public virtual string ServerInit(int? fromConnId)
    {
        ServerFromConnId = fromConnId;

        ApplyBaseVisual();
        ApplyTeamColors();

        return "";
    }

    public virtual void ClientInit(string effectDataJson)
    {
        ApplyBaseVisual();
        ApplyTeamColors();
    }

    #endregion

    #region Base Visual

    protected virtual void ApplyBaseVisual()
    {
        if (icon)
        {
            EffectTarget.SetDisplayIcon(icon);
            var color = GetResolvedIconColor();
            color.a = 1;
            EffectTarget.SetDisplayIconColor(color);
        }

        if (!string.IsNullOrEmpty(defaultText))
        {
            EffectTarget.SetDisplayText(defaultText);
        }

        transform.localScale = Vector3.one * EffectTarget.Tank.VisualSize;
    }

    protected virtual Color GetResolvedIconColor()
    {
        switch (iconColorMode)
        {
            case TeamColorMode.FromTeam:
                {
                    Color? c = GetTeamColor(FromTeamId);
                    return c ?? fixedIconColor;
                }

            default:
                return fixedIconColor;
        }
    }

    #endregion

    #region Team Colors

    protected virtual void ApplyTeamColors()
    {
        Color? fromColor = GetTeamColor(FromTeamId);

        if (fromColor.HasValue)
        {
            ApplyColorSet(
                fromColor.Value,
                fromTeamSpriteRenderers,
                fromTeamTrails,
                fromTeamParticles
            );
        }

    }

    protected virtual Color? GetTeamColor(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return null;

        if (!GameTanksManager.Singleton)
            return null;

        var team = GameTanksManager.Singleton.GetTeam(teamId);
        if (team == null)
            return null;

        return team.color;
    }

    protected virtual void ApplyColorSet(
        Color color,
        List<SpriteRenderer> spriteRenderers,
        List<TrailRenderer> trails,
        List<ParticleSystem> particles
    )
    {
        if (spriteRenderers != null)
        {
            foreach (var sr in spriteRenderers)
            {
                if (sr == null) continue;

                Color c = color;
                c.a = sr.color.a;
                sr.color = c;
            }
        }

        if (trails != null)
        {
            foreach (var trail in trails)
            {
                if (trail == null) continue;
                trail.SetStartColorKeepAlpha(color);
            }
        }

        if (particles != null)
        {
            foreach (var ps in particles)
            {
                if (ps == null) continue;
                ps.SetStartColorKeepAlpha(color);
            }
        }
    }

    #endregion

    public abstract string ServerReapply();

    public abstract void ServerOnRemove();

    public abstract void ClientSetEffectData(string effectDataJson);

    public abstract void ClientOnRemove();

    protected abstract void TheUpdate();

    private void Update()
    {
        TheUpdate();
    }
}