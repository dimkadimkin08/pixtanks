using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class StructureStatusEffects : NetworkBehaviour
{

    public static string GenerateEffectId(string ownerId, string resourcePath)
    {
        return $"{ownerId}_{resourcePath[(resourcePath.LastIndexOf('/') + 1)..]}";
    }

    [SerializeField]
    private Transform effectObjectsParent;

    [Header("Structure")]

    [SerializeField]
    private NetworkStructure structure;

    [Header("Display")]

    [SerializeField]
    private StatusEffectDisplayObject[] effectsDisplayObjects = { };
    [SerializeField]
    private GameObject moreEffectsIndicator;

    private readonly SyncDictionary<string, StatusEffectIdentity> _statusEffects = new();

    private readonly Dictionary<string, StructureStatusEffect> _localStatusEffectInstances = new();

    private readonly Dictionary<string, StatusEffectDisplayInfo> _statusEffectDisplay = new();

    public override void OnStartClient()
    {
        if (isServer)
            return;
        _statusEffects.OnAdd += ClientOnStatusEffectAdded;
        _statusEffects.OnSet += ClientOnStatusEffectChanged;
        _statusEffects.OnRemove += ClientOnStatusEffectRemoved;
        _statusEffects.OnClear += ClientOnStatusEffectCleared;
    }

    public override void OnStopClient()
    {
        if (isServer)
            return;
        _statusEffects.OnAdd -= ClientOnStatusEffectAdded;
        _statusEffects.OnSet -= ClientOnStatusEffectChanged;
        _statusEffects.OnRemove -= ClientOnStatusEffectRemoved;
        _statusEffects.OnClear -= ClientOnStatusEffectCleared;
    }

    private void Start()
    {
        UpdateStatusEffectInstances();
        UpdateStatusEffectDisplay();
    }

    private void OnEnable()
    {
        UpdateStatusEffectInstances();
        UpdateStatusEffectDisplay();
    }

    private void ClientOnStatusEffectAdded(string key)
    {
        ClientApplyStatusEffect(key, _statusEffects[key]);
    }

    private void ClientOnStatusEffectChanged(string key, StatusEffectIdentity oldValue)
    {
        var newValue = _statusEffects[key];
        if (newValue.statusEffectName != oldValue.statusEffectName)
        {
            ClientRemoveStatusEffect(key);
            ClientApplyStatusEffect(key, newValue);
        }
        else if (_localStatusEffectInstances.ContainsKey(key))
            _localStatusEffectInstances[key].ClientSetEffectData(newValue.instanceDataJson);
        else
            ClientApplyStatusEffect(key, newValue);
    }

    private void ClientOnStatusEffectRemoved(string key, StatusEffectIdentity oldValue)
    {
        ClientRemoveStatusEffect(key);
    }

    private void ClientOnStatusEffectCleared()
    {
        UpdateStatusEffectInstances();
    }

    private void UpdateStatusEffectInstances()
    {
        foreach (var keyToRemove in _localStatusEffectInstances.Keys.Where(instanceId => !_statusEffects.ContainsKey(instanceId)).ToArray())
            if (isServer)
                ServerRemoveStatusEffect(keyToRemove);
            else
                ClientRemoveStatusEffect(keyToRemove);

        foreach (var effectToApply in _statusEffects.Where(effect => !_localStatusEffectInstances.ContainsKey(effect.Key)))
            if (isServer)
                ServerApplyStatusEffect(effectToApply.Key, effectToApply.Value.statusEffectName);
            else
                ClientApplyStatusEffect(effectToApply.Key, effectToApply.Value);
    }

    [Client]
    private void ClientApplyStatusEffect(string id, StatusEffectIdentity statusEffect)
    {
        if (_localStatusEffectInstances.TryGetValue(id, out var instance))
        {
            instance.ClientSetEffectData(statusEffect.instanceDataJson);
            return;
        }
        var effectAsset = GameStatusEffectsManager.Singleton.GetStatusEffect(statusEffect.statusEffectName);
        if (effectAsset == default || effectAsset.type != GameStatusEffectsManager.EffectType.Structure)
            return;
        var effectObject = Instantiate(effectAsset.prefab, effectObjectsParent);
        if (!effectObject.TryGetComponent(out StructureStatusEffect statusEffectInstance))
        {
            Destroy(effectObject);
            return;
        }
        statusEffectInstance.EffectTarget = new StructureStatusEffect.StatusEffectTarget
        {
            Structure = structure,
            GetTime = () => NetworkTime.time,
            SetDisplayIcon = (sprite) => SetStatusEffectIcon(id, sprite),
            SetDisplayIconColor = (color) => SetStatusEffectIconColor(id, color),
            SetDisplayText = (text) => SetStatusEffectDisplayText(id, text),
        };
        _localStatusEffectInstances[id] = statusEffectInstance;
        statusEffectInstance.ClientInit(statusEffect.instanceDataJson);
    }

    [Client]
    private void ClientRemoveStatusEffect(string id)
    {
        if (!_localStatusEffectInstances.TryGetValue(id, out var instance))
            return;
        instance.ClientOnRemove();
        Destroy(_localStatusEffectInstances[id].gameObject);
        _localStatusEffectInstances.Remove(id);
        _statusEffectDisplay.Remove(id);
        UpdateStatusEffectDisplay();
    }

    [Server]
    private void ServerSendEffectData(string id, string effectDataJson)
    {
        if (!_statusEffects.TryGetValue(id, out var statusEffect))
            return;
        if (effectDataJson == "" && effectDataJson == statusEffect.instanceDataJson)
            return;
        var updatedEffect = _statusEffects[id].Copy();
        updatedEffect.instanceDataJson = effectDataJson;
        _statusEffects[id] = updatedEffect;
    }

    [Server]
    public void ServerApplyStatusEffect(string id, string statusEffectName)
    {
        var statusEffectIdentity = new StatusEffectIdentity { statusEffectName = statusEffectName };
        if (_localStatusEffectInstances.TryGetValue(id, out var instance))
        {
            statusEffectIdentity.instanceDataJson = instance.ServerReapply();
            ServerSendEffectData(id, statusEffectIdentity.instanceDataJson);
        }
        else
        {
            var effectAsset = GameStatusEffectsManager.Singleton.GetStatusEffect(statusEffectName);
            if (effectAsset == default || effectAsset.type != GameStatusEffectsManager.EffectType.Structure)
                return;
            var effectObject = Instantiate(effectAsset.prefab, effectObjectsParent);
            if (!effectObject.TryGetComponent(out StructureStatusEffect statusEffectInstance))
            {
                Destroy(effectObject);
                return;
            }
            statusEffectInstance.EffectTarget = new StructureStatusEffect.StatusEffectTarget
            {
                Structure = structure,
                GetTime = () => NetworkTime.time,
                ServerSendEffectData = data => ServerSendEffectData(id, data),
                ServerRemoveEffect = () => ServerRemoveStatusEffect(id),
                SetDisplayIcon = (sprite) => SetStatusEffectIcon(id, sprite),
                SetDisplayIconColor = (color) => SetStatusEffectIconColor(id, color),
                SetDisplayText = (text) => SetStatusEffectDisplayText(id, text),
            };
            _localStatusEffectInstances[id] = statusEffectInstance;
            statusEffectIdentity.instanceDataJson = statusEffectInstance.ServerInit();
        }
        _statusEffects[id] = statusEffectIdentity;
    }

    [Server]
    public void ServerRemoveStatusEffect(string id)
    {
        if (_localStatusEffectInstances.ContainsKey(id))
        {
            _localStatusEffectInstances[id].ServerOnRemove();
            Destroy(_localStatusEffectInstances[id].gameObject);
            _localStatusEffectInstances.Remove(id);
        }
        _statusEffects.Remove(id);
        _statusEffectDisplay.Remove(id);
        UpdateStatusEffectDisplay();
    }

    private void SetStatusEffectIcon(string id, Sprite icon)
    {
        if (!_statusEffectDisplay.ContainsKey(id))
        {
            _statusEffectDisplay[id] = new();
        }
        _statusEffectDisplay[id].icon = icon;
        UpdateStatusEffectDisplay();
    }

    private void SetStatusEffectIconColor(string id, Color color)
    {
        if (!_statusEffectDisplay.ContainsKey(id))
        {
            _statusEffectDisplay[id] = new();
        }
        _statusEffectDisplay[id].color = color;
        UpdateStatusEffectDisplay();
    }

    private void SetStatusEffectDisplayText(string id, string text)
    {
        if (!_statusEffectDisplay.ContainsKey(id))
        {
            _statusEffectDisplay[id] = new();
        }
        _statusEffectDisplay[id].text = text;
        UpdateStatusEffectDisplay();
    }

    private void UpdateStatusEffectDisplay()
    {
        var displayEffects = _statusEffectDisplay
            .Where(pair => _localStatusEffectInstances.ContainsKey(pair.Key))
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value)
            .Where(displayInfo => displayInfo.icon);
        var isMore = displayEffects.Count() > effectsDisplayObjects.Length;
        var displayEffectsArray = displayEffects.Take(effectsDisplayObjects.Length).ToArray();
        for (int i = 0; i < effectsDisplayObjects.Length; i++)
        {
            var visible = displayEffectsArray.Length > i;
            if (effectsDisplayObjects[i].iconSpriteRenderer)
            {
                effectsDisplayObjects[i].iconSpriteRenderer.sprite = !visible ? null : displayEffectsArray[i].icon;
                effectsDisplayObjects[i].iconSpriteRenderer.color = !visible ? Color.white : displayEffectsArray[i].color;
            }
            if (effectsDisplayObjects[i].text)
                effectsDisplayObjects[i].text.text = !visible ? "" : displayEffectsArray[i].text;
        }
        if (moreEffectsIndicator)
        {
            moreEffectsIndicator.SetActive(isMore);
        }
    }

    public bool ContainsEffect(string id)
    {
        return _statusEffects.ContainsKey(id);
    }

    [Serializable]
    public struct StatusEffectIdentity
    {
        public string statusEffectName;
        public string instanceDataJson;

        public StatusEffectIdentity Copy()
        {
            return new StatusEffectIdentity
            {
                statusEffectName = statusEffectName,
                instanceDataJson = instanceDataJson
            };
        }
    }

    [Serializable]
    private class StatusEffectDisplayInfo
    {
        public Sprite icon;
        public Color color;
        public string text;
    }

    [Serializable]
    private class StatusEffectDisplayObject
    {
        public SpriteRenderer iconSpriteRenderer;
        public TextMeshPro text;
    }
}
