using Mirror;
using System;
using System.Linq;
using UnityEngine;
public class ZapAttack : Attack
{

    [Header("Parameters")]
    [SerializeField]
    private ushort baseHeal;
    [SerializeField]
    private ushort baseLifeSteal;

    [Header("(other parameters)")]
    [SerializeField]
    private float lifeTime;
    [SerializeField]
    private float attackRadius;
    [SerializeField]
    private Vector2 radiusOffset;
    [SerializeField]
    private LayerMask targetLayers;

    [Header("Components")]

    [SerializeField]
    private Rigidbody2D attackRigidbody;
    [SerializeField]
    private Transform hitPointTransform;

    [SyncVar(hook = nameof(HookData))]
    private ZapData _data;

    private double _serverStartTime;

    public override void OnStartServer()
    {
        _serverStartTime = NetworkTime.time;
        if (!isClient)
            ApplyData(_data);
    }

    public override void OnStartClient()
    {
        ApplyData(_data);
    }

    private void HookData(ZapData old, ZapData current)
    {
        ApplyData(current);
    }

    private void ApplyData(ZapData data)
    {
        hitPointTransform.position = data.hitPosition;
    }

    private void Update()
    {
        if (isServer && _serverStartTime > 0 && NetworkTime.time - _serverStartTime > lifeTime)
            this.DestroyNetObject();
    }

    [Server]
    public override void ServerInitImpl()
    {
        var team = GameTanksManager.Singleton.GetTeam(currentTeamId);

        ServerOwnerRigidbody.TryGetComponent(out ZapTankShootingRules zapRules);
        ServerOwnerRigidbody.TryGetComponent(out NetworkTank ownerTank);

        bool canHeal = zapRules && zapRules.CurrentStacks > 0;

        Vector2 startPos = (Vector2)transform.position +
                           (Vector2)transform.up * radiusOffset.y +
                           (Vector2)transform.right * radiusOffset.x;

        var hits = Physics2D.OverlapCircleAll(startPos, attackRadius, targetLayers.value);

        Rigidbody2D target = null;
        float bestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            var rb = hit.attachedRigidbody;
            if (!rb || rb == ServerOwnerRigidbody)
                continue;

            bool valid = false;

            if (rb.TryGetComponent(out NetworkStructure _))
            {
                valid = true;
            }
            else if (rb.TryGetComponent(out NetworkTank otherTank))
            {
                valid = team.enemies.Contains(otherTank.Parameters.TeamId) ||
                        (canHeal && otherTank.Parameters.TeamId == team.id && otherTank.Parameters.CurrentHp < otherTank.Parameters.MaxHp);
            }

            if (!valid)
                continue;

            float distance = Vector2.Distance(transform.position, rb.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                target = rb;
            }
        }

        if (target)
        {
            var hitResult = ServerTryHit(target);
            if (hitResult.tankHit)
            {
                ownerTank.Parameters.ServerApplyHealing(baseLifeSteal, bypass: true, fromPlayerConnId: ServerOwnerConnId);
                zapRules.ServerRestoreStack();
            }
            else if (canHeal && target.TryGetComponent(out NetworkTank tank)
                && currentTeamId == tank.Parameters.TeamId
                && !ServerEnemies.Contains(tank.Parameters.TeamId)
                && tank.Parameters.CurrentHp < tank.Parameters.MaxHp)
            {
                tank.Parameters.ServerApplyHealing(baseHeal, bypass: true, fromPlayerConnId: ServerOwnerConnId);
                zapRules.ServerConsumeStack();
            }
        }

        var oldData = _data;
        _data = new ZapData
        {
            hitPosition = target ? target.position : startPos
        };

        if (isServerOnly)
            HookData(oldData, _data);
    }


    [Serializable]
    private struct ZapData
    {
        public Vector2 hitPosition;
    }
}
