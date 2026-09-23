using Mirror;
using UnityEngine;

public class MortarProjectileAttack : Attack
{
    [Header("Distance")]
    [SyncVar]
    public float targetDistance = 10f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 14f;

    [Header("Lifetime & Speed")]
    [SerializeField] private float minLifetime = 0.25f;
    [SerializeField] private float maxLifetime = 2.5f;
    [SerializeField] private AnimationCurve distanceToLifetimeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Space]
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Scale")]
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 3f;
    [SerializeField]
    private AnimationCurve scaleCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    [SerializeField] private bool affectChildrenScale = false;

    [Header("Components")]
    [SerializeField] private Rigidbody2D attackRigidbody;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectObject;

    [Header("Spawn On Target")]
    [SerializeField] private GameObject[] spawnOnHitPrefabs;

    private ServerMortarData _serverData;

    private Vector2 _startPosition;
    private double _startTime;

    private Vector3 _baseScale;

    private float TimePassed =>
        _startTime > 0 ? (float)(NetworkTime.time - _startTime) : 0f;

    private float TargetDistance => Mathf.Clamp(targetDistance, minDistance, maxDistance);

    private float LifeTime
    {
        get
        {
            float distance01 = Mathf.InverseLerp(
                minDistance,
                maxDistance,
                TargetDistance);

            float curveValue = Mathf.Clamp01(
                distanceToLifetimeCurve.Evaluate(distance01));

            return Mathf.Lerp(
                minLifetime,
                maxLifetime,
                curveValue);
        }
    }

    public override void OnStartServer()
    {
        _startPosition = transform.position;
        _startTime = NetworkTime.time;
        CacheBaseScale();
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            _startPosition = transform.position;
            _startTime = NetworkTime.time;
        }

        CacheBaseScale();
    }

    public override void OnStopClient()
    {
        if (gameObject.scene.isLoaded &&
            !AppPlatform.IsHeadless &&
            hitEffectObject &&
            NetworkClient.active &&
            NetworkTank.LocalPlayer)
        {
            hitEffectObject.transform.parent = null;
        }

        if (hitEffectObject && hitEffectObject.transform.parent == null)
        {
            hitEffectObject.SetActive(true);
            hitEffectObject.transform.position =
                _startPosition + (Vector2)transform.up * TargetDistance;
        }
    }

    private void Update()
    {
        float t = TimePassed;
        float lifeTime = LifeTime;

        ApplyScale(t, lifeTime);

        if (isServer && t >= lifeTime)
        {
            if (!_serverData.TargetReached)
            {
                ReachTarget();
            }
        }
        else if (isClientOnly)
        {
            transform.position =
                CalculatePosition(
                    _startPosition,
                    (Vector2)transform.up,
                    t,
                    lifeTime,
                    TargetDistance);
        }
    }

    private void FixedUpdate()
    {
        if (!isServer || _serverData.TargetReached)
            return;

        Vector2 pos = CalculatePosition(
            _startPosition,
            transform.up,
            TimePassed,
            LifeTime,
            TargetDistance);

        attackRigidbody.MovePosition(pos);
    }

    private Vector2 CalculatePosition(
        Vector2 startPosition,
        Vector2 direction,
        float time,
        float lifeTime,
        float distance)
    {
        direction.Normalize();

        float normalizedTime = Mathf.Clamp01(time / lifeTime);
        float progress = Mathf.Clamp01(speedCurve.Evaluate(normalizedTime));

        return startPosition + direction * (distance * progress);
    }

    private void ApplyScale(float time, float lifeTime)
    {
        if (lifeTime <= 0f)
            return;

        float progress = Mathf.Clamp01(time / lifeTime);
        Vector3 targetScale = Vector3.one * Mathf.Lerp(minScale, maxScale, Mathf.Clamp01(scaleCurve.Evaluate(progress)));

        if (affectChildrenScale)
            transform.localScale = targetScale;
        else
            transform.localScale = new Vector3(targetScale.x, targetScale.y, _baseScale.z);
    }

    private void CacheBaseScale()
    {
        _baseScale = transform.localScale;
    }

    private void ReachTarget()
    {
        SpawnOnHitPrefabsIfNeeded();

        _serverData.TargetReached = true;
        this.DestroyNetObject();
    }

    private void SpawnOnHitPrefabsIfNeeded()
    {
        foreach (var spawnOnHitPrefab in spawnOnHitPrefabs)
        {
            if (!spawnOnHitPrefab)
                continue;
            var prefab =
                spawnOnHitPrefab == gameObject
                    ? ServerSelfPrefab
                    : spawnOnHitPrefab;

            Vector3 spawnPos =
                _startPosition + (Vector2)transform.up * TargetDistance;

            var obj = Instantiate(prefab, spawnPos, transform.rotation);

            if (obj.TryGetComponent(out Attack attack))
            {
                attack.ServerInit(
                    prefab,
                    ServerOwnerConnId,
                    ServerOwnerNetId,
                    currentTeamId,
                    ServerDamageMultiplier,
                    ServerOwnerRigidbody);
            }

            NetworkServer.Spawn(obj);
        }
    }

    protected override Vector2 GetTankKnockback(Transform target)
    {
        return Vector2.zero;
    }

    protected override Vector2 GetStructKnockback(Transform target)
    {
        return Vector2.zero;
    }

    [Server]
    public override void ServerInitImpl()
    {
        _serverData = new ServerMortarData
        {
            TargetReached = false
        };

        float speed = Mathf.Max(0f, speedCurve.Evaluate(0f));
        attackRigidbody.linearVelocity = (Vector2)transform.up * speed;
    }

    private class ServerMortarData
    {
        public bool TargetReached;
    }
}