using System;
using System.Linq;
using UnityEngine;
public class GameStatusEffectsManager : MonoBehaviour
{

    public static GameStatusEffectsManager Singleton;

    [Header("Status Effects")]
    public StatusEffectAsset[] statusEffects;

    public enum EffectType { Tank, Structure }

    private void Awake()
    {
        Singleton = this;
    }

    public StatusEffectAsset GetStatusEffect(string statusEffectName)
    {
        return statusEffects.FirstOrDefault(statusEffect => statusEffect.name == statusEffectName);
    }

    [Serializable]
    public class StatusEffectAsset
    {
        public string name;
        public GameObject prefab;
        public EffectType type;
        [Tooltip("Prevent this effect from being applied via commands")]
        public bool preventCommands;
    }
}