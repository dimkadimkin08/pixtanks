using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
public class TankShoot : NetworkBehaviour
{

    [Header("Tank")]

    [SerializeField]
    private NetworkTank tank;

    [Header("Rules (Optional)")]

    [SerializeField]
    private TankShootingRules shootingRules;

    [Header("Patterns")]

    [SerializeField]
    private AttackPattern[] attackPatterns;
    [SerializeField]
    private PatternMode patternMode = PatternMode.RoundRobin;

    [Header("Cursor Options")]
    [SerializeField]
    private bool enableCursorRange = false;
    [SerializeField]
    private float minCursorRange = 1;
    [SerializeField]
    private float maxCursorRange = 10;

    [SyncVar(hook = nameof(HookPatternData))]
    private PatternData _patternData = new();

    [SyncVar]
    private float _reloadProgress = 1;

    private bool _serverShootingBlocked;
    private bool _serverShootingEnabled;
    [SyncVar]
    private Vector2 _cursorPos;
    private Vector2 _serverRawCursorPos;

    public Vector2 CursorPos => _cursorPos;
    public int PatternIndex => _patternData.index;
    public int NextPatternIndex => _patternData.nextIndex;
    public double LastShotTime => _patternData.executedAt;
    public AttackPattern CurrentPattern => attackPatterns[PatternIndex];
    public AttackPattern NextPattern => attackPatterns[NextPatternIndex];
    public float ReloadTime => attackPatterns[NextPatternIndex].loadTime;
    public float ReloadProgress => _reloadProgress;
    public bool IsShooting => NetworkTime.time < LastShotTime + CurrentPattern.duration;
    public bool IsReloading => _reloadProgress < 1f;

    public TankShootingRules ShootingRules => shootingRules;

    public event Action<PatternData> OnShoot;

    private enum PatternMode { RoundRobin, Random }

    public override void OnStartServer()
    {
        _serverRawCursorPos = transform.position + transform.up;
        ServerUpdateCursorPos();
    }

    private void Update()
    {
        if (isServer)
        {
            ServerUpdateCursorPos();
            UpdateReloadProgress();

            if (_serverShootingEnabled && !_serverShootingBlocked && !IsShooting && !IsReloading)
            {
                if (!shootingRules || shootingRules.IsAbleToShoot)
                    ServerShoot();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isServer)
            ServerUpdateCursorPos();
    }

    private void LateUpdate()
    {
        if (isServer)
            ServerUpdateCursorPos();
    }

    private void OnDisable()
    {
        if (isServer)
            StopAllCoroutines();
    }

    private void OnDestroy()
    {
        OnShoot = null;
    }

    [Server]
    private void ServerUpdateCursorPos()
    {
        _cursorPos = !enableCursorRange
                ? _serverRawCursorPos
                : (Vector2)transform.position + (
                    (_serverRawCursorPos - (Vector2)transform.position).normalized *
                    Mathf.Clamp((_serverRawCursorPos - (Vector2)transform.position).magnitude, minCursorRange, maxCursorRange)
                );
    }

    [Server]
    private void UpdateReloadProgress()
    {
        if (_reloadProgress == 1)
            return;

        float reloadTime = ReloadTime;
        if (reloadTime <= 0f)
        {
            _reloadProgress = 1f;
            return;
        }

        _reloadProgress += (Time.deltaTime / reloadTime) * tank.Parameters.ReloadMultiplier;
        _reloadProgress = Mathf.Clamp01(_reloadProgress);
    }

    [Server]
    private int GetNextPatternIndex(int index)
    {
        return patternMode switch
        {
            PatternMode.Random => UnityEngine.Random.Range(0, attackPatterns.Length),
            PatternMode.RoundRobin => index + 1 < attackPatterns.Length ? index + 1 : 0,
            _ => 0
        };
    }

    [Server]
    private void ServerShoot()
    {
        if (shootingRules && !shootingRules.IsAbleToShoot)
            return;
        if (tank.Death.IsDead || IsShooting || _serverShootingBlocked || IsReloading || attackPatterns.Length == 0)
            return;
        var index = Math.Clamp(_patternData.nextIndex, 0, attackPatterns.Length - 1);
        var oldData = _patternData;
        _patternData = new PatternData
        {
            index = index,
            nextIndex = GetNextPatternIndex(index),
            executedAt = NetworkTime.time
        };
        _reloadProgress = 0f;
        if (isServer)
            HookPatternData(oldData, _patternData);
        StartCoroutine(ServerExecutePattern(attackPatterns[index]));
    }

    [Server]
    private void ServerSpawnAttack(AttackPattern.Element patternElement)
    {
        if (tank.Death.IsDead || _serverShootingBlocked)
            return;
        if (patternElement.selfPush != Vector2.zero)
        {
            var push = transform.up * patternElement.selfPush.y + transform.right * patternElement.selfPush.x;
            tank.Movement.ServerAddForce(push, bypassResistance: true);
        }
        if (!patternElement.prefab)
            return;
        var startPoint = patternElement.startPoint ? patternElement.startPoint : transform;
        var position = startPoint.position;
        position += startPoint.up * patternElement.startOffset.y;
        position += startPoint.right * patternElement.startOffset.x;
        var rotation = patternElement.startPoint ? patternElement.startPoint.rotation : transform.rotation;
        rotation *= Quaternion.Euler(0, 0, patternElement.angle);
        var attackObject = Instantiate(patternElement.prefab, position, rotation);
        if (!attackObject.TryGetComponent(out Attack attack))
        {
            Destroy(attackObject);
            return;
        }
        if (attack is MortarProjectileAttack mortarAttack)
        {
            mortarAttack.targetDistance = Vector2.Distance(tank.Rigidbody.position, CursorPos);
        }
        attack.ServerInit(patternElement.prefab, tank);
        NetworkServer.Spawn(attackObject);
    }

    [Server]
    private IEnumerator ServerExecutePattern(AttackPattern pattern)
    {
        var waitForFrame = new WaitForEndOfFrame();
        var elements = pattern.elements.OrderBy(element => element.spawnDelay).ToList();
        float timePassed = 0;
        int totalCount = elements.Count;
        ushort i = 0;
        while (!tank.Death.IsDead && elements.Count > 0 && i < totalCount)
        {
            while (timePassed < elements[0].spawnDelay)
            {
                yield return waitForFrame;
                if (tank.Death.IsDead)
                    break;
                timePassed += Time.deltaTime;
            }

            if (tank.Death.IsDead)
                break;

            var passed = timePassed;
            var executeQuery = elements.Where(element => passed >= element.spawnDelay);
            var count = 0;
            foreach (var element in executeQuery)
            {
                ServerSpawnAttack(element);
                count++;
            }
            if (count > 0)
                elements.RemoveRange(0, count);
            i++;
        }
        yield return null;
    }

    [Server]
    public void ServerSetCursorPos(Vector2 cursorPos)
    {
        _serverRawCursorPos = cursorPos;
        ServerUpdateCursorPos();
    }

    [Server]
    public Vector2 ServerGetCursorPos()
    {
        return _cursorPos;
    }

    [Server]
    public void ServerSetShootingBlocked(bool shootingBlocked)
    {
        _serverShootingBlocked = shootingBlocked;
    }


    [Server]
    public void ServerSetShooting(bool shootingEnabled)
    {
        if (shootingEnabled && !_serverShootingEnabled)
            ServerShoot();
        _serverShootingEnabled = shootingEnabled;
    }

    private void HookPatternData(PatternData _, PatternData patternData)
    {
        if (!tank.Death.IsDead && NetworkTime.time - patternData.executedAt < attackPatterns[patternData.index].duration)
        {
            OnShoot?.Invoke(patternData);
        }
    }

    [Serializable]
    public class PatternData
    {
        public int index;
        public int nextIndex;
        public double executedAt;
    }

    [Serializable]
    public class AttackPattern
    {
        public Element[] elements = Array.Empty<Element>();
        public float loadTime;
        public float duration;

        [Serializable]
        public class Element
        {
            public GameObject prefab;
            public Transform startPoint;
            public Vector2 startOffset;
            public Vector2 selfPush;
            public float angle;
            public float spawnDelay;
        }
    }

}
