using Mirror;
using System;
using System.Linq;
using UnityEngine;

public class ProjectileAttack : Attack
{
    [Header("Parameters")]
    [SerializeField] private float speed;
    [SerializeField] private float lifeTime;
    [SerializeField] private float speedLoss;
    [SerializeField] private float minSpeed = 0;
    [SerializeField] private float maxSpeed = 999;
    [SerializeField] private bool ignoreAllyTanks = true;

    [Header("Ricochet")]
    //[SerializeField] private Collider2D ricochetCollider;
    [SerializeField] private bool ricochetEnabled = false;
    [SerializeField] private GameObject ricochetPrefab;
    [SerializeField] private int ricochetCount = 0;
    [SerializeField] private float ricochetBackYOffset = 0.2f;
    [SerializeField] private float ricochetFowardYOffset = 0.1f;
    [SerializeField] private float ricochetMinAngle = 15f;

    [Header("Knockback")]
    [SerializeField] private float tankKnockbackPushForce;
    [SerializeField] private float rigidbodyKnockbackForce;

    [Header("Components")]
    [SerializeField] private Rigidbody2D attackRigidbody;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectObject;

    [Header("Spawn On Hit")]
    [SerializeField] private GameObject spawnOnHitPrefab;

    private ServerProjectileData _serverData;

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
        if (!isServer)
        {
            _startPosition = transform.position;
            _startTime = NetworkTime.time;
        }
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
            float t = TimePassed;

            hitEffectObject.SetActive(true);
            hitEffectObject.transform.position = ClientCalculatePosition(_startPosition, (Vector2)transform.up, t, speed, speedLoss, minSpeed, maxSpeed);
        }
    }

    private void Update()
    {
        float t = TimePassed;

        if (isServer && t > lifeTime)
        {
            if (!_serverData.HitExecuted)
            {
                SpawnOnHitPrefabIfNeeded();
                _serverData.HitExecuted = true;
                this.DestroyNetObject();
            }
        }
        else if (isClientOnly)
        {
            transform.position = ClientCalculatePosition(_startPosition, (Vector2)transform.up, t, speed, speedLoss, minSpeed, maxSpeed);
        }
    }

    private Vector2 ClientCalculatePosition(
        Vector2 startPosition,
        Vector2 direction,
        float time,
        float speed,
        float speedLoss,
        float minSpeed,
        float maxSpeed)
    {
        direction = direction.normalized;

        float a = -speedLoss;
        float currentSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        float distance = 0f;

        if (Mathf.Approximately(a, 0f))
        {
            distance = currentSpeed * time;
            return startPosition + direction * distance;
        }

        float targetSpeed = a > 0 ? maxSpeed : minSpeed;

        float tLimit = (targetSpeed - currentSpeed) / a;

        if (tLimit < 0f)
            tLimit = 0f;

        if (time <= tLimit)
        {
            distance = currentSpeed * time + 0.5f * a * time * time;
        }
        else
        {
            distance =
                currentSpeed * tLimit +
                0.5f * a * tLimit * tLimit;

            distance += targetSpeed * (time - tLimit);
        }

        return startPosition + direction * distance;
    }

    private void FixedUpdate()
    {
        if (isServer && !_serverData.HitExecuted)
        {
            _serverData.CurrentSpeed -= speedLoss * Time.fixedDeltaTime * 0.5f;
            _serverData.CurrentSpeed = Mathf.Clamp(_serverData.CurrentSpeed, minSpeed, maxSpeed);
            attackRigidbody.linearVelocity = (Vector2)transform.up * _serverData.CurrentSpeed;
            _serverData.CurrentSpeed -= speedLoss * Time.fixedDeltaTime * 0.5f;
            _serverData.CurrentSpeed = Mathf.Clamp(_serverData.CurrentSpeed, minSpeed, maxSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !isServer || _serverData == null || _serverData.HitExecuted)
            return;

        var otherRb = other.attachedRigidbody;

        if (otherRb == ServerOwnerRigidbody)
            return;

        if (otherRb && otherRb.TryGetComponent(out Attack otherAttack))
        {
            if ((currentTeamId != otherAttack.GetTeamId() ||
                 ServerEnemies.Contains(otherAttack.GetTeamId())) &&
                otherAttack.ServerGetOwnerNetId() != ServerGetOwnerNetId())
            {
                _serverData.ToughnessLeft =
                    _serverData.ToughnessLeft.Sub(otherAttack.Toughness);
            }

            if (_serverData.ToughnessLeft > 0)
                return;
        }
        else
        {
            var hitResult = ServerTryHit(other);

            if (ignoreAllyTanks &&
                hitResult.wasNonEnemyTank &&
                !hitResult.tankHit &&
                hitResult.wasNonEnemyTank.Parameters.TeamId == currentTeamId)
                return;

            bool hitWall = !hitResult.tankHit && !hitResult.structHit;

            if (hitWall && CanRicochet())
            {
                SpawnRicochet(other);
                _serverData.HitExecuted = true;
                this.DestroyNetObject();
                return;
            }
        }

        SpawnOnHitPrefabIfNeeded();

        _serverData.HitExecuted = true;
        this.DestroyNetObject();
    }

    private bool CanRicochet()
    {
        if (!ricochetEnabled || !ricochetPrefab)
            return false;

        return ricochetCount == -1 || ricochetCount > 0;
    }

    private void SpawnOnHitPrefabIfNeeded()
    {
        if (!spawnOnHitPrefab)
            return;

        var prefab = spawnOnHitPrefab == gameObject ? ServerSelfPrefab : spawnOnHitPrefab;
        var obj = Instantiate(prefab, transform.position, transform.rotation);

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

    private void SpawnRicochet(Collider2D wall)
    {
        Vector2 dir = GetRicochetDir(wall);

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, dir);

        var prefab = ricochetPrefab == gameObject ? ServerSelfPrefab : ricochetPrefab;
        var pos = transform.position;
        pos += (Vector3)dir * ricochetFowardYOffset;
        pos -= transform.up * ricochetBackYOffset;
        var obj = Instantiate(prefab, pos, rotation);

        if (obj.TryGetComponent(out ProjectileAttack projectile))
        {
            projectile.ricochetEnabled = ricochetEnabled;
            projectile.ricochetPrefab = prefab;

            if (ricochetCount > 0)
                projectile.ricochetCount = ricochetCount - 1;
            else
                projectile.ricochetCount = ricochetCount; // 0 или -1

            projectile.ServerInit(
                prefab,
                ServerOwnerConnId,
                ServerOwnerNetId,
                currentTeamId,
                ServerDamageMultiplier,
                ServerOwnerRigidbody);
        }
        else if (obj.TryGetComponent(out Attack attack))
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

    private Vector2 GetRicochetDir(Collider2D wall)
    {
        Vector2 hitPoint = transform.position;
        Vector2 closest = wall.ClosestPoint(hitPoint);
        Vector2 normal = (hitPoint - closest).normalized;

        Vector2 forward = transform.up;

        if (normal == Vector2.zero)
        {
            if (Mathf.Abs(forward.x) > Mathf.Abs(forward.y))
                normal = new Vector2(Mathf.Sign(forward.x), 0f);
            else
                normal = new Vector2(0f, Mathf.Sign(forward.y));
        }

        Vector2 ricochet = Vector2.Reflect(forward, normal).normalized;

        float angle = Vector2.Angle(forward, ricochet);

        if (angle < ricochetMinAngle)
        {
            float sign = Mathf.Sign(
                forward.x * normal.y - forward.y * normal.x
            );

            if (sign == 0f)
                sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;

            ricochet = Quaternion.Euler(0f, 0f, ricochetMinAngle * sign) * forward;
            ricochet.Normalize();
        }

        return ricochet;
    }

    private Vector2 GetKnockbackDir(Transform target)
    {
        var distance = (Vector2)target.position - (Vector2)transform.position;
        return ((Vector2)transform.up + distance).normalized;
    }

    protected override Vector2 GetTankKnockback(Transform target)
    {
        return tankKnockbackPushForce * GetKnockbackDir(target);
    }

    protected override Vector2 GetStructKnockback(Transform target)
    {
        return rigidbodyKnockbackForce * GetKnockbackDir(target);
    }

    [Server]
    public override void ServerInitImpl()
    {
        _serverData = new ServerProjectileData
        {
            CurrentSpeed = speed,
            ToughnessLeft = toughness,
            HitExecuted = false
        };

        attackRigidbody.linearVelocity = (Vector2)transform.up * speed;
    }

    private class ServerProjectileData
    {
        public float CurrentSpeed;
        public ushort ToughnessLeft;
        public bool HitExecuted;
    }
}