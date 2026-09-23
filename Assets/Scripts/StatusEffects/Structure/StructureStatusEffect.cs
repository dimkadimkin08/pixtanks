using System;
using UnityEngine;
public abstract class StructureStatusEffect : MonoBehaviour
{

    public Sprite icon;
    public Color iconColor = Color.white;

    public class StatusEffectTarget
    {

        public NetworkStructure Structure;

        public Func<double> GetTime;

        public Action<string> ServerSendEffectData;
        public Action ServerRemoveEffect;
        public Action<Sprite> SetDisplayIcon;
        public Action<Color> SetDisplayIconColor;
        public Action<string> SetDisplayText;
    }

    public StatusEffectTarget EffectTarget;

    public virtual string ServerInit()
    {
        if (icon)
        {
            EffectTarget.SetDisplayIcon(icon);
            EffectTarget.SetDisplayIconColor(iconColor);
        }
        return "";
    }

    public virtual void ClientInit(string effectDataJson)
    {
        if (icon)
        {
            EffectTarget.SetDisplayIcon(icon);
            EffectTarget.SetDisplayIconColor(iconColor);
        }
    }

    public abstract string ServerReapply();

    public abstract void ServerOnRemove();

    public abstract void ClientSetEffectData(string effectDataJson);

    public abstract void ClientOnRemove();

}
