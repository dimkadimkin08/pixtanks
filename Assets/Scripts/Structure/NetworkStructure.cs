using System;
using System.Collections;
using Mirror;
using UnityEngine;
public class NetworkStructure : NetworkBehaviour
{

    [SerializeField]
    private Rigidbody2D structureRigidbody;
    [SerializeField]
    private StructureStatusEffects statusEffects;

    [Space]

    [SerializeField]
    private GameObject deathEffectObject;
    [SerializeField]
    private AudioSource damageSound;
    [SerializeField]
    private GameObject damageEffectObject;
    [SerializeField]
    private float damageEffectDuration = 1;

    [Space]

    [SerializeField]
    private Loot[] lootList;

    [Space]

    [SerializeField, SyncVar(hook = nameof(HookCurrentHp))]
    private ushort currentHp;
    [SerializeField, SyncVar(hook = nameof(HookMaxHp))]
    private ushort maxHp;

    [SyncVar(hook = nameof(HookDeathState))]
    private bool _deathState;

    public Rigidbody2D Rigidbody => structureRigidbody;

    public StructureStatusEffects StatusEffects => statusEffects;

    public ushort MaxHp
    {
        get => maxHp;
        set
        {
            var oldValue = maxHp;
            maxHp = value;
            if (authority && !isClient)
                HookMaxHp(oldValue, maxHp);
            if (maxHp < CurrentHp)
                CurrentHp = maxHp;
        }
    }

    public ushort CurrentHp
    {
        get => currentHp;
        set
        {
            var oldValue = currentHp;
            currentHp = value < maxHp ? value : maxHp;
            if (authority && !isClient)
                HookCurrentHp(oldValue, currentHp);
        }
    }

    public event Action<ushort, ushort> OnCurrentHpSet;
    public event Action<ushort, ushort> OnMaxHpSet;

    private void HookCurrentHp(ushort old, ushort current) => OnCurrentHpSet?.Invoke(old, current);
    private void HookMaxHp(ushort old, ushort current) => OnMaxHpSet?.Invoke(old, current);

    private void HookDeathState(bool old, bool current)
    {
        if (current && old != current && deathEffectObject)
        {
            deathEffectObject.transform.parent = null;
            deathEffectObject.SetActive(true);
        }
    }

    public override void OnStartServer()
    {
        OnCurrentHpSet += ServerOnCurrentHpChanged;
    }

    public override void OnStopServer()
    {
        OnCurrentHpSet -= ServerOnCurrentHpChanged;
    }

    public override void OnStartClient()
    {
        OnCurrentHpSet += ClientOnCurrentHpChanged;
    }

    public override void OnStopClient()
    {
        OnCurrentHpSet -= ClientOnCurrentHpChanged;
    }

    private void OnDestroy()
    {
        OnCurrentHpSet = null;
        OnMaxHpSet = null;
    }

    [Server]
    private void ServerOnCurrentHpChanged(ushort old, ushort current)
    {
        if (!_deathState && current == 0)
            ServerKillStructure();
    }

    [Server]
    private void ServerSpawnLoot()
    {
        int lootSeed = Mathf.RoundToInt(UnityEngine.Random.value * 100);
        foreach (var loot in lootList)
        {
            for (int i = 0; i < loot.count; i++)
            {
                var impulse = GameItemsManager.GetSpreadImpulse(
                    i,
                    loot.count,
                    loot.impulseDuration,
                    seed: lootSeed
                );

                GameItemsManager.Singleton.ServerSpawnItem(
                    loot.itemId,
                    transform.position,
                    impulse,
                    loot.impulseStrength
                );
            }
        }
    }

    [Client]
    private void ClientOnCurrentHpChanged(ushort old, ushort current)
    {
        if (!_deathState && current > 0 && current < old)
        {
            if (damageSound && damageSound.enabled)
                damageSound.Play();
            StopCoroutine(nameof(ShowDamageEffect));
            StartCoroutine(ShowDamageEffect());
        }
    }

    private IEnumerator ShowDamageEffect()
    {
        if (damageEffectObject)
        {
            damageEffectObject.SetActive(false);
            damageEffectObject.SetActive(true);
        }
        yield return new WaitForSeconds(damageEffectDuration);
        if (damageEffectObject)
            damageEffectObject.SetActive(false);
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        this.DestroyNetObject();
    }

    [Server]
    public void ServerDealDamage(ushort damage)
    {
        CurrentHp = CurrentHp.Sub(damage);
    }

    [Server]
    public bool ServerIsDead()
    {
        return _deathState;
    }

    [Server]
    public void ServerKillStructure(bool preventLootDrop = false)
    {
        if (_deathState)
            return;
        _deathState = true;
        if (deathEffectObject)
        {
            deathEffectObject.transform.parent = null;
            deathEffectObject.SetActive(true);
        }
        StartCoroutine(DestroyAfterDelay());
        if (!preventLootDrop)
            ServerSpawnLoot();
    }

    [Serializable]
    private class Loot
    {
        public float impulseStrength;
        public float impulseDuration;
        public int count;
        public string itemId;
    }
}