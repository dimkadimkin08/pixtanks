using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.UI.GridLayoutGroup;

public abstract class Attack : NetworkBehaviour
{

    public GameObject ServerSelfPrefab { get; private set; }

    [Header("Team Color Apply")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private TrailRenderer[] trailRenderers;
    [SerializeField] private LineRenderer[] lineRenderers;

    [Header("Base Parameters")]
    [SerializeField] protected ushort baseDamage;
    [SerializeField] protected ushort toughness;
    [SerializeField] protected string[] statusEffectResources;
    [SerializeField] protected string[] structureStatusEffectResources;

    [SyncVar(hook = nameof(HookCurrentTeam))]
    public string currentTeamId;

    public int? ServerOwnerConnId { get; private set; }
    public uint ServerOwnerNetId { get; private set; }
    public string ServerTeamId { get; private set; }
    public float ServerDamageMultiplier { get; private set; }
    public Rigidbody2D ServerOwnerRigidbody { get; private set; }

    protected string[] ServerEnemies;
    protected Dictionary<string, string> ServerEffects;
    protected Dictionary<string, string> ServerStructureEffects;

    public ushort Toughness => toughness;

    private Color DisplayTeamColor =>
        currentTeamId != ""
            ? GameTanksManager.Singleton.GetTeam(currentTeamId).color
            : new Color(0, 0, 0, 0);

    public override void OnStartClient()
    {
        ApplyTeamColor(DisplayTeamColor);
    }

    private void HookCurrentTeam(string oldValue, string newValue)
    {
        ApplyTeamColor(DisplayTeamColor);
    }

    public void ServerInit(GameObject selfPrefab, NetworkTank owner)
    {
        ServerInit(
            selfPrefab,
            owner.connectionToClient?.connectionId,
            owner.netId,
            owner.Parameters.TeamId,
            owner.Parameters.DamageMultiplier,
            owner.Rigidbody);
    }

    [Server]
    public void ServerInit(GameObject selfPrefab, int? ownerConnId, uint ownerNetId, string ownerTeamId,
                           float damageMultiplier, Rigidbody2D ownerRigidbody)
    {
        ServerSelfPrefab = selfPrefab;
        currentTeamId = ownerTeamId;
        if (isClient)
            ApplyTeamColor(DisplayTeamColor);
        ServerOwnerConnId = ownerConnId;
        ServerOwnerNetId = ownerNetId;
        ServerDamageMultiplier = damageMultiplier;
        ServerOwnerRigidbody = ownerRigidbody;
        ServerEnemies = GameTanksManager.Singleton.GetTeam(ownerTeamId).enemies;
        ServerEffects = statusEffectResources
            .ToDictionary(name => TankStatusEffects.GenerateEffectId(ownerNetId.ToString(), name));
        ServerStructureEffects = structureStatusEffectResources
            .ToDictionary(name => TankStatusEffects.GenerateEffectId(ownerNetId.ToString(), name));
        ServerInitImpl();
    }

    public string GetTeamId() => currentTeamId;

    [Server]
    public uint ServerGetOwnerNetId() => ServerOwnerNetId;

    public abstract void ServerInitImpl();

    protected HitResult ServerTryHit(
        Collider2D targetCollider,
        Func<NetworkTank, bool> tankCheck = null,
        Func<NetworkStructure, bool> structCheck = null)
    {
        if (!targetCollider.attachedRigidbody)
            return new HitResult();
        return ServerTryHit(targetCollider.attachedRigidbody, tankCheck, structCheck);
    }

    protected HitResult ServerTryHit(
        Rigidbody2D targetRigidbody,
        Func<NetworkTank, bool> tankCheck = null,
        Func<NetworkStructure, bool> structCheck = null)
    {
        var result = new HitResult();

        if (!targetRigidbody || targetRigidbody == ServerOwnerRigidbody)
            return result;

        var damage = baseDamage.Mul(ServerDamageMultiplier);

        if (targetRigidbody.TryGetComponent(out NetworkTank tank))
        {
            if (!ServerEnemies.Contains(tank.Parameters.TeamId))
            {
                result.wasNonEnemyTank = tank;
            }
            else
            {
                result.wasEnemyTank = tank;
                if (tankCheck == null || tankCheck(tank))
                {
                    result.tankHit = true;
                    tank.Parameters.ServerDealDamage(damage, fromPlayerConnId: ServerOwnerConnId);

                    if (tank.Parameters.CurrentHp > 0)
                    {
                        foreach (var effect in ServerEffects)
                            tank.StatusEffects.ServerApplyStatusEffect(effect.Key, effect.Value,
                                fromConnId: ServerOwnerConnId, fromTeamId: currentTeamId);

                        var knockback = GetTankKnockback(tank.transform);
                        if (knockback != Vector2.zero)
                            tank.Movement.ServerAddForce(knockback);
                    }
                }
            }
        }

        if (targetRigidbody.TryGetComponent(out NetworkStructure structure))
        {
            result.wasStructure = structure;
            if (structCheck == null || structCheck(structure))
            {
                result.structHit = true;
                structure.ServerDealDamage(damage);

                if (structure.CurrentHp > 0)
                {
                    foreach (var effect in ServerStructureEffects)
                        structure.StatusEffects.ServerApplyStatusEffect(effect.Key, effect.Value);

                    var knockback = GetStructKnockback(structure.transform);
                    if (structure.Rigidbody && knockback != Vector2.zero)
                        structure.Rigidbody.AddForce(knockback);
                }
            }
        }

        return result;
    }

    protected virtual Vector2 GetTankKnockback(Transform target) => Vector2.zero;
    protected virtual Vector2 GetStructKnockback(Transform target) => Vector2.zero;

    protected void ApplyTeamColor(Color color)
    {
        if (AppPlatform.IsHeadless)
            return;

        foreach (var r in spriteRenderers)
            if (r)
                r.color = new Color(color.r, color.g, color.b, r.color.a);

        foreach (var p in particleSystems)
            if (p)
                p.SetStartColorKeepAlpha(color);

        foreach (var t in trailRenderers)
            if (t)
                t.SetStartColorKeepAlpha(color);

        foreach (var l in lineRenderers)
            if (l)
                l.SetStartColorKeepAlpha(color);
    }

    protected struct HitResult
    {
        public NetworkTank wasNonEnemyTank;
        public NetworkTank wasEnemyTank;
        public NetworkStructure wasStructure;
        public bool tankHit;
        public bool structHit;
    }
}
