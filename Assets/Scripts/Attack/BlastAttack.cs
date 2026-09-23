using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
public class BlastAttack : Attack
{
    [Header("Parameters")]
    [SerializeField] private float speed;
    [SerializeField] private float lifeTime;

    [Space]

    [SerializeField] private bool followOwnerTank;

    [Header("Knockback")]
    [SerializeField] private Transform knockbackCenter;
    [SerializeField] private float tankKnockbackPushForce;
    [SerializeField] private float rigidbodyKnockbackForce;

    [Header("Distance Knockback Multiplier")]
    [SerializeField] private bool useDistanceMultiplier = false;

    [SerializeField] private float minDistance = 0f;
    [SerializeField] private float maxDistance = 5f;

    [SerializeField] private AnimationCurve distanceMultiplierCurve = AnimationCurve.Linear(0, 1, 1, 1);

    [Header("Multi Trigger")]
    [SerializeField] private bool hitMultipleTimes;
    [SerializeField] private float hitInterval = 0;
    [SerializeField] private float hitLimit = 1;

    [Header("Components")]
    [SerializeField] private Rigidbody2D attackRigidbody;

    [Header("Events")]
    [SerializeField] private UnityEvent<Attack> serverOnHitEnemyAttack;

    private ServerBlastData _serverData = null;

    [SyncVar] private Vector2 _followPosition;
    [SyncVar] private float _rotation;

    private Vector2 _serverFollowTankOffset;
    private Transform _serverFollowTank;
    private Vector2 _startPosition;
    private double _startTime;

    private float TimePassed =>
        _startTime > 0 ? (float)(NetworkTime.time - _startTime) : 0;

    public override void OnStartServer()
    {
        _startPosition = transform.position;
        _startTime = NetworkTime.time;
    }

    public override void OnStartClient()
    {
        _startPosition = transform.position;
        _startTime = NetworkTime.time;
    }

    private void Update()
    {
        if (isServer && hitMultipleTimes)
        {
            var targets = _serverData.Hits.Keys.ToArray();
            foreach (var target in targets)
            {
                var hitData = _serverData.Hits[target];
                if (hitData.isTouching && NetworkTime.time - hitData.timestamp >= hitInterval)
                    ServerExecuteHit(target);
            }
        }

        var timePassed = TimePassed;

        if (timePassed > lifeTime)
        {
            if (isServer)
                this.DestroyNetObject();
        }
        else
        {
            if (isServer)
            {
                if (_serverFollowTank)
                {
                    _startPosition = _serverFollowTank.position + _serverFollowTank.transform.rotation * _serverFollowTankOffset;
                    _rotation = _serverFollowTank.eulerAngles.z;
                    attackRigidbody.position = _startPosition + (Vector2)transform.up * (speed * timePassed);
                    attackRigidbody.rotation = _rotation;
                    _followPosition = attackRigidbody.position;
                }
                else
                {
                    attackRigidbody.position = _startPosition + (Vector2)transform.up * (speed * timePassed);
                }
            }
            if (isClient)
            {
                if (_followPosition != Vector2.zero)
                {
                    float lerpTime = 0.04f;
                    float t = Time.deltaTime / lerpTime;

                    attackRigidbody.position = Vector2.Lerp(
                        attackRigidbody.position,
                        _followPosition,
                        t
                    );

                    attackRigidbody.rotation = Mathf.LerpAngle(
                        attackRigidbody.rotation,
                        _rotation,
                        t
                    );
                }
                else
                {
                    attackRigidbody.position = _startPosition + (Vector2)transform.up * (speed * timePassed);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer || _serverData == null)
            return;
        
        var otherRigidbody = other.attachedRigidbody;

        if (!otherRigidbody || otherRigidbody == ServerOwnerRigidbody)
            return;

        if (_serverData.Hits.ContainsKey(otherRigidbody))
        {
            _serverData.Hits[otherRigidbody].isTouching = true;
            return;
        }

        if (otherRigidbody.TryGetComponent(out Attack otherAttack))
        {
            if ((currentTeamId != otherAttack.GetTeamId() ||
                ServerEnemies.Contains(otherAttack.GetTeamId())) &&
               otherAttack.ServerGetOwnerNetId() != ServerGetOwnerNetId())
            {
                serverOnHitEnemyAttack?.Invoke(otherAttack);
            }
        }

        ServerExecuteHit(otherRigidbody);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isServer || _serverData == null)
            return;

        var otherRigidbody = other.attachedRigidbody;

        if (!otherRigidbody || otherRigidbody == ServerOwnerRigidbody)
            return;

        if (_serverData.Hits.ContainsKey(otherRigidbody))
            _serverData.Hits[otherRigidbody].isTouching = false;
    }

    [Server]
    private void ServerExecuteHit(Rigidbody2D otherRigidbody)
    {
        ServerTryHit(otherRigidbody,
            tankCheck: tank =>
            {
                int count = _serverData.Hits.TryGetValue(otherRigidbody, out var oldData)
                    ? oldData.count + 1
                    : 1;

                if ((hitMultipleTimes && count <= hitLimit) || (!hitMultipleTimes && count <= 1))
                {
                    _serverData.Hits[otherRigidbody] = new()
                    {
                        count = count,
                        timestamp = NetworkTime.time,
                        isTouching = true
                    };
                    return true;
                }
                return false;
            },
            structCheck: structure =>
            {
                int count = _serverData.Hits.TryGetValue(otherRigidbody, out var oldData)
                    ? oldData.count + 1
                    : 1;

                if ((hitMultipleTimes && count <= hitLimit) || (!hitMultipleTimes && count <= 1))
                {
                    _serverData.Hits[otherRigidbody] = new()
                    {
                        count = count,
                        timestamp = NetworkTime.time,
                        isTouching = true
                    };
                    return true;
                }
                return false;
            });
    }

    private float GetDistanceMultiplier(Transform target)
    {
        if (!useDistanceMultiplier)
            return 1f;

        var kbCenter = knockbackCenter
            ? (Vector2)knockbackCenter.position
            : (Vector2)transform.position;

        float distance = Vector2.Distance(target.position, kbCenter);

        if (maxDistance <= minDistance)
            return 1f;

        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        return distanceMultiplierCurve.Evaluate(t);
    }

    private Vector2 GetKnockbackDir(Transform target)
    {
        var kbCenter = knockbackCenter
            ? (Vector2)knockbackCenter.position
            : (Vector2)transform.position;
        var distance = (Vector2)target.position - kbCenter;
        return ((Vector2)transform.up + distance).normalized;
    }

    protected override Vector2 GetTankKnockback(Transform target)
    {
        float multiplier = GetDistanceMultiplier(target);
        return tankKnockbackPushForce * multiplier * GetKnockbackDir(target);
    }

    protected override Vector2 GetStructKnockback(Transform target)
    {
        float multiplier = GetDistanceMultiplier(target);
        return rigidbodyKnockbackForce * multiplier * GetKnockbackDir(target);
    }

    [Server]
    public override void ServerInitImpl()
    {
        _serverData = new ServerBlastData();
        if (followOwnerTank)
        {
            var ownerId = ServerGetOwnerNetId();
            foreach (var tank in GameTanksManager.Singleton.serverTankInstances)
            {
                if (tank && tank.netId == ownerId)
                {
                    _serverFollowTank = tank.transform;
                    _serverFollowTankOffset = Quaternion.Inverse(tank.transform.rotation) * (transform.position - tank.transform.position);
                    break;
                }
            }
        }
    }

    private class ServerBlastData
    {
        public readonly Dictionary<Rigidbody2D, HitData> Hits = new();

        public class HitData
        {
            public int count;
            public double timestamp;
            public bool isTouching;
        }
    }
}
