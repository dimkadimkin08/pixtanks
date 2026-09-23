using JetBrains.Annotations;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class HitscanAttack : Attack
{

    [Header("Parameters")]
    [SerializeField]
    private LayerMask penetrateLayers;
    [SerializeField]
    private bool useAttackWidth;
    [SerializeField]
    private float attackWidth;
    [SerializeField]
    private float attackDistance;

    [Header("Transform")]
    public float distanceScaleMultiplier;
    public float distancePositionMultiplier;

    [Header("Lifetime (Visuals)")]
    [SerializeField]
    private float lifeTime;
    [SerializeField]
    private float damageStateTime;

    [Header("Knockback")]
    [SerializeField]
    private float tankKnockbackPushForce;
    [SerializeField]
    private float rigidbodyKnockbackForce;

    [Header("Components")]
    [SerializeField]
    private Rigidbody2D attackRigidbody;

    [SyncVar(hook = nameof(HookData))]
    private HitscanData _data;

    [CanBeNull]
    private ServerHitscanData _serverData;

    public override void OnStartServer()
    {
        if (!isClient)
            ApplyData(_data);
    }

    public override void OnStartClient()
    {
        ApplyData(_data);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer || _serverData == null || NetworkTime.time > _serverData.ActiveUntil)
            return;

        var otherRigidbody = other.attachedRigidbody;

        if (!otherRigidbody || otherRigidbody == ServerOwnerRigidbody)
            return;

        if (_serverData.Hits.Contains(otherRigidbody))
            return;

        var hitResult = ServerTryHit(otherRigidbody);

        if (hitResult.tankHit || hitResult.structHit)
            _serverData.Hits.Add(otherRigidbody);
    }

    private void Update()
    {
        if (isServer && _serverData.initTime > 0 && NetworkTime.time - _serverData.initTime > lifeTime)
            this.DestroyNetObject();
    }

    private void HookData(HitscanData old, HitscanData current)
    {
        ApplyData(current);
    }

    private void ApplyData(HitscanData data)
    {
        transform.SetPositionAndRotation(data.center, Quaternion.Euler(0, 0, data.angle));
        transform.localScale = new Vector3(transform.localScale.x, data.length * distanceScaleMultiplier, transform.localScale.z);
    }

    [Server]
    private List<RaycastHit2D> ServerHitscanCast()
    {
        var hits = new List<RaycastHit2D>();
        var contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(attackRigidbody.CalculateLayerMask());
        if (useAttackWidth)
        {
            var distance = attackDistance - attackWidth;
            var radius = attackWidth / 2;
            Physics2D.CircleCast(transform.position, radius, transform.up, contactFilter, hits, distance);
        }
        else
        {
            Physics2D.Raycast(transform.position, transform.up, contactFilter, hits, attackDistance);
        }
        return hits;
    }

    protected override Vector2 GetTankKnockback(Transform target)
    {
        return tankKnockbackPushForce * transform.up;
    }

    protected override Vector2 GetStructKnockback(Transform target)
    {
        return rigidbodyKnockbackForce * transform.up;
    }

    public override void ServerInitImpl()
    {
        _serverData = new ServerHitscanData
        {
            initTime = NetworkTime.time,
            ActiveUntil = NetworkTime.time + damageStateTime
        };
        var hits = ServerHitscanCast();
        var obstacleIndex = -1;
        for (var i = 0; i < hits.Count; i++)
        {
            var hitRigidbody = hits[i].rigidbody;
            var hitResult = ServerTryHit(hitRigidbody,
                tankCheck: (tank) =>
                {
                    if (!_serverData.Hits.Contains(hitRigidbody))
                    {
                        _serverData.Hits.Add(hitRigidbody);
                        return true;
                    }
                    return false;
                },
                structCheck: (structure) =>
                {
                    if (!_serverData.Hits.Contains(hitRigidbody))
                    {
                        _serverData.Hits.Add(hitRigidbody);
                        return true;
                    }
                    return false;
                });
            var hitLayer = hitRigidbody ? hitRigidbody.gameObject.layer : hits[i].collider.gameObject.layer;
            if (!penetrateLayers.Contains(hitLayer))
            {
                obstacleIndex = i;
                break;
            }
        }
        var length = obstacleIndex >= 0 ? Vector3.Distance(transform.position, hits[obstacleIndex].point) : attackDistance;
        length = Mathf.Max(length, 0.2f);
        var center = transform.position + length * distancePositionMultiplier / 2 * transform.up;
        var oldData = _data;
        _data = new HitscanData
        {
            angle = transform.eulerAngles.z,
            center = center,
            length = length
        };
        if (isServerOnly)
            HookData(oldData, _data);
    }

    [Serializable]
    private class ServerHitscanData
    {
        public double initTime;
        public readonly List<Rigidbody2D> Hits = new();
        public double ActiveUntil;
    }

    [Serializable]
    private struct HitscanData
    {
        public Vector2 center;
        public float length;
        public float angle;
    }
}
