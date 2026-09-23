using System;
using System.Linq;
using Mirror;
using UnityEngine;
public class TankParameters : NetworkBehaviour
{
    public readonly SyncDictionary<string, ushort> shields = new();

    [SerializeField, SyncVar(hook = nameof(HookDisplayName))]
    private string displayName;
    [SerializeField, SyncVar(hook = nameof(HookTeamId))]
    private string teamId;
    [SerializeField, SyncVar(hook = nameof(HookGears))]
    private ushort gears;
    [SerializeField, SyncVar(hook = nameof(HookMaxHp))]
    private ushort maxHp = 20;
    [SerializeField, SyncVar(hook = nameof(HookCurrentHp))]
    private ushort currentHp = 20;
    [SerializeField, SyncVar(hook = nameof(HookSpeedMultiplier))]
    private float speedMultiplier = 1;
    [SerializeField, SyncVar(hook = nameof(HookDamageMultiplier))]
    private float damageMultiplier = 1;
    [SerializeField, SyncVar(hook = nameof(HookReloadMultiplier))]
    private float reloadMultiplier = 1;
    [SerializeField, SyncVar(hook = nameof(HookHealMultiplier))]
    private float healMultiplier = 1;
    [SerializeField, SyncVar(hook = nameof(HookIncomingDamageMultiplier))]
    private float incomingDamageMultiplier = 1;

    [Space]

    [SerializeField]
    private float spawnInvulnerabilityTime = 2;

    [SyncVar]
    private double spawnTimestamp;

    public float SpawnInvulnerabilityTime => spawnInvulnerabilityTime;
    public double SpawnTimestamp => spawnTimestamp;

    public string DisplayName
    {
        get => displayName;
        set
        {
            var oldValue = displayName;
            displayName = value;
            if (authority && !isClient)
                HookDisplayName(oldValue, displayName);
        }
    }

    public string TeamId
    {
        get => teamId;
        set
        {
            var oldValue = teamId;
            teamId = value;
            if (authority && !isClient)
                HookTeamId(oldValue, teamId);
        }
    }

    public ushort Gears
    {
        get => gears;
        set
        {
            var oldValue = gears;
            gears = value;
            if (authority && !isClient)
                HookGears(oldValue, gears);
        }
    }

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

    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set
        {
            var oldValue = speedMultiplier;
            speedMultiplier = value;
            if (authority && !isClient)
                HookSpeedMultiplier(oldValue, speedMultiplier);
        }
    }

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set
        {
            var oldValue = damageMultiplier;
            damageMultiplier = value;
            if (authority && !isClient)
                HookDamageMultiplier(oldValue, damageMultiplier);
        }
    }

    public float ReloadMultiplier
    {
        get => reloadMultiplier;
        set
        {
            var oldValue = reloadMultiplier;
            reloadMultiplier = value;
            if (authority && !isClient)
                HookReloadMultiplier(oldValue, reloadMultiplier);
        }
    }

    public float HealMultiplier
    {
        get => healMultiplier;
        set
        {
            var oldValue = healMultiplier;
            healMultiplier = value;
            if (authority && !isClient)
                HookHealMultiplier(oldValue, healMultiplier);
        }
    }

    public float IncomingDamageMultiplier
    {
        get => incomingDamageMultiplier;
        set
        {
            var oldValue = incomingDamageMultiplier;
            incomingDamageMultiplier = value;
            if (authority && !isClient)
                HookIncomingDamageMultiplier(oldValue, incomingDamageMultiplier);
        }
    }

    public event Action<string, string> OnDisplayNameSet;
    public event Action<string, string> OnTeamIdSet;
    public event Action<ushort, ushort> OnGearsSet;
    public event Action<ushort, ushort> OnMaxHpSet;
    public event Action<ushort, ushort> OnCurrentHpSet;
    public event Action<float, float> OnSpeedMultiplierSet;
    public event Action<float, float> OnDamageMultiplierSet;
    public event Action<float, float> OnReloadMultiplierSet;
    public event Action<float, float> OnHealMultiplierSet;
    public event Action<float, float> OnIncomingDamageMultiplierSet;

    public event Action<HitData> ClientOnHitTaken;

    public event Action<int, ushort> ServerOnDamageTakenFromPlayer;
    public event Action<int, ushort> ServerOnHealTakenFromPlayer;

    private void HookDisplayName(string old, string current) => OnDisplayNameSet?.Invoke(old, current);
    private void HookTeamId(string old, string current) => OnTeamIdSet?.Invoke(old, current);
    private void HookGears(ushort old, ushort current) => OnGearsSet?.Invoke(old, current);
    private void HookMaxHp(ushort old, ushort current) => OnMaxHpSet?.Invoke(old, current);
    private void HookCurrentHp(ushort old, ushort current) => OnCurrentHpSet?.Invoke(old, current);
    private void HookSpeedMultiplier(float old, float current) => OnSpeedMultiplierSet?.Invoke(old, current);
    private void HookDamageMultiplier(float old, float current) => OnDamageMultiplierSet?.Invoke(old, current);
    private void HookReloadMultiplier(float old, float current) => OnReloadMultiplierSet?.Invoke(old, current);
    private void HookHealMultiplier(float old, float current) => OnHealMultiplierSet?.Invoke(old, current);
    private void HookIncomingDamageMultiplier(float old, float current) => OnIncomingDamageMultiplierSet?.Invoke(old, current);

    private void OnDestroy()
    {
        OnDisplayNameSet = null;
        OnTeamIdSet = null;
        OnGearsSet = null;
        OnMaxHpSet = null;
        OnCurrentHpSet = null;
        OnSpeedMultiplierSet = null;
        OnDamageMultiplierSet = null;
        OnReloadMultiplierSet = null;
        OnHealMultiplierSet = null;
        OnIncomingDamageMultiplierSet = null;
    }

    public override void OnStartServer()
    {
        spawnTimestamp = NetworkTime.time;
    }

    [Server]
    public void ServerApplyHealing(ushort healAmount, bool bypass = false, int? fromPlayerConnId = null, Vector2? hitPos = null)
    {
        var heal = UShortExt.RoundAndClamp(healAmount * (bypass ? 1 : healMultiplier));
        var hpBefore = CurrentHp;

        CurrentHp = CurrentHp.Add(heal);

        var actualHeal = UShortExt.RoundAndClamp(Mathf.Min(heal, maxHp - hpBefore));

        if (fromPlayerConnId is int connId)
            ServerOnHealTakenFromPlayer?.Invoke(connId, actualHeal);

        if (actualHeal > 0)
        {
            RpcHitTaken(new()
            {
                hpValue = actualHeal,
                fromPlayerConnId = fromPlayerConnId,
                fromPlayerNetId =
                    fromPlayerConnId.HasValue &&
                    NetworkServer.connections.TryGetValue(fromPlayerConnId.Value, out var p) &&
                    p.identity
                        ? p.identity.netId
                        : null
            });
        }
    }

    [Server]
    public void ServerDealDamage(ushort damageAmount, bool bypass = false, int? fromPlayerConnId = null, Vector2? hitPos = null)
    {
        if (NetworkTime.time - spawnTimestamp < spawnInvulnerabilityTime)
            return;

        var wasAlive = currentHp > 0;
        var damage = UShortExt.RoundAndClamp(damageAmount * (bypass ? 1f : incomingDamageMultiplier));
        var initialDamage = damage;
        var hpBefore = currentHp;

        while (damage > 0 && shields.Any(s => s.Value > 0))
        {
            var shield = shields.Where(s => s.Value > 0).OrderBy(s => s.Key).First();
            var absorbed = Math.Min(damage, shield.Value);
            shields[shield.Key] = shield.Value.Sub(absorbed);
            damage -= absorbed;
        }

        foreach (var key in shields.Where(p => p.Value == 0).Select(p => p.Key).ToList())
            shields.Remove(key);

        if (damage > 0)
            CurrentHp = CurrentHp.Sub(damage);

        var absorbedByShields = initialDamage - damage;
        var hpDamage = Mathf.Min(damage, hpBefore);
        var totalDealt = UShortExt.RoundAndClamp(absorbedByShields + hpDamage);

        if (wasAlive && fromPlayerConnId is int connId)
            ServerOnDamageTakenFromPlayer?.Invoke(connId, totalDealt);


        if (totalDealt > 0)
        {
            RpcHitTaken(new()
            {
                isKill = wasAlive && currentHp == 0,
                hpValue = -totalDealt,
                fromPlayerConnId = fromPlayerConnId,
                fromPlayerNetId =
                    fromPlayerConnId != null &&
                    NetworkServer.connections.TryGetValue(fromPlayerConnId.Value, out var p) &&
                    p.identity
                        ? p.identity.netId
                        : null
            });
        }
    }

    [Server]
    public void ServerAddGears(ushort gearsAmount)
    {
        Gears = Gears.Add(gearsAmount);
    }

    [Server]
    public void ServerRemoveGears(ushort gearsAmount)
    {
        Gears = Gears.Sub(gearsAmount);
    }

    [ClientRpc(includeOwner = true)]
    public void RpcHitTaken(HitData hitData)
    {
        ClientOnHitTaken?.Invoke(hitData);
    }

    [Serializable]
    public struct HitData
    {
        public int hpValue;
        public int? fromPlayerConnId;
        public uint? fromPlayerNetId;
        public bool isKill;
        public Vector2? hitPos;
    }
}
