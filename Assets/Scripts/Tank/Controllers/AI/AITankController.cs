using UnityEngine;
using Random = UnityEngine.Random;
public class AITankController
{

    private readonly NetworkTank _tank;
    private readonly TankAISettings _settings;
    private readonly TankScanner _scanner;
    private readonly Transform _gunCenter;

    private string DebugLogPrefix => $"TankAI (netId: {_tank.netId}): ";

    private float _timeUntilNextPhase;
    private Phase _currentPhase;

    public bool DebugLogEnabled;

    private enum Phase { Idle, IdleMove, BattleMoveDelay, BattleMove, PrepareShoot, Shoot }

    public AITankController(NetworkTank tank, TankAISettings settings, TankAIScannerSettings scannerSettings, Transform gunCenter = null)
    {
        _tank = tank;
        _settings = settings;
        _scanner = new TankScanner(tank.Parameters.TeamId, tank.Rigidbody, scannerSettings);
        _gunCenter = gunCenter;
    }

    private void PrintDebugLog(object message)
    {
        Debug.Log(DebugLogPrefix + message, _tank.gameObject);
    }

    private void ChooseNewPhase()
    {
        if (!_scanner.TargetEnemy)
            _currentPhase = _currentPhase == Phase.Idle ? Phase.IdleMove : Phase.Idle;
        else if (_currentPhase == Phase.PrepareShoot)
            if (CheckShootAngle())
                _currentPhase = Phase.Shoot;
            else
                _currentPhase = Phase.BattleMove;
        else if (_currentPhase == Phase.BattleMove && CheckShootDistance())
            _currentPhase = Phase.PrepareShoot;
        else if (_currentPhase != Phase.BattleMoveDelay && RandomUtils.Chance(_settings.battleMoveDelayChance))
            _currentPhase = Phase.BattleMoveDelay;
        else
            _currentPhase = Phase.BattleMove;
        if (DebugLogEnabled)
            PrintDebugLog($" Phase: {_currentPhase}");
    }

    private bool CheckShootDistance()
    {
        return _settings.maxShootDistance > Vector2.Distance(_tank.Rigidbody.position, _scanner.TargetEnemy.position);
    }

    private bool CheckShootAngle()
    {
        if (!_scanner.TargetEnemy)
            return false;
        Vector2 toTarget = (Vector2)_scanner.TargetEnemy.position - (Vector2)_tank.transform.position;
        return Vector2.Angle(_tank.transform.up, toTarget) <= _settings.maxShootAngle;
    }

    private bool CheckPhaseInterrupted()
    {
        return _currentPhase switch
        {
            Phase.Idle => _scanner.TargetEnemy,
            Phase.IdleMove => _scanner.TargetEnemy,
            Phase.BattleMove => !_tank.Movement.IsMoving,
            Phase.PrepareShoot => CheckShootAngle() && _timeUntilNextPhase < _settings.maxShootDelay / 1.5f,
            _ => false
        };
    }

    private float ExecutePhase()
    {
        return _currentPhase switch
        {
            Phase.Idle => IdlePhase(),
            Phase.IdleMove => IdleMovePhase(),
            Phase.BattleMoveDelay => BattleMoveDelayPhase(),
            Phase.BattleMove => BattleMovePhase(),
            Phase.PrepareShoot => PrepareShootPhase(),
            Phase.Shoot => ShootPhase(),
            _ => 1
        };
    }

    private float IdlePhase()
    {
        _tank.Shoot.ServerSetShooting(false);
        _tank.Movement.ServerSetDrive(0);
        return Random.Range(_settings.minIdleMoveDelay, _settings.maxIdleMoveDelay);
    }

    private float IdleMovePhase()
    {
        _tank.Shoot.ServerSetShooting(false);
        _tank.Movement.ServerSetDirection(Random.insideUnitCircle);
        _tank.Movement.ServerSetDrive(1);
        return Random.Range(_settings.minIdleMoveTime, _settings.maxIdleMoveTime);
    }

    private float BattleMoveDelayPhase()
    {
        _tank.Shoot.ServerSetShooting(false);
        _tank.Movement.ServerSetDrive(0);
        return Random.Range(_settings.minBattleMoveDelay, _settings.maxBattleMoveDelay);
    }

    private float BattleMovePhase()
    {
        _tank.Shoot.ServerSetShooting(false);

        var isMovingBack = RandomUtils.Chance(_settings.chanceToMoveBackBattleMove);

        _tank.Movement.ServerSetDrive(isMovingBack ? (short)-1 : (short)1);

        var vectorToTarget = (Vector2)_scanner.TargetEnemy.position - _tank.Rigidbody.position;

        var isMinimalDistance = vectorToTarget.magnitude < _settings.minimalDistance;
        var isSafe = !isMinimalDistance && vectorToTarget.magnitude > _settings.safeDistance;

        if (isMinimalDistance && !isMovingBack)
            vectorToTarget = -vectorToTarget;

        var maxAngle = CheckShootDistance() ? _settings.normalBattleMoveAngle : _settings.chaseBattleMoveAngle;

        var randomAngle = Random.Range(-maxAngle, maxAngle);

        var duration = Random.Range(_settings.minBattleMoveTime, _settings.maxBattleMoveTime);
        if (!isSafe)
        {
            randomAngle = Mathf.Sign(randomAngle) * Mathf.Lerp(Mathf.Abs(randomAngle), maxAngle, 0.6f);
            duration *= 1.75f;
        }

        var finalDirection = Quaternion.Euler(0, 0, randomAngle) * vectorToTarget.normalized;
        _tank.Movement.ServerSetDirection(finalDirection);

        return duration;
    }

    private float PrepareShootPhase()
    {
        _tank.Shoot.ServerSetShooting(false);
        var gunPosition = (_gunCenter != null ? (Vector2)_gunCenter.position : _tank.Rigidbody.position);
        _tank.Movement.ServerSetDirection((Vector2)_scanner.TargetEnemy.position - gunPosition);
        _tank.Shoot.ServerSetCursorPos(_scanner.TargetEnemy.position);
        _tank.Movement.ServerSetDrive(RandomUtils.Chance(_settings.chanceToMoveDuringShoot)
            ? RandomUtils.Chance(_settings.chanceToMoveBackDuringShoot) ? (short)-1 : (short)1
            : (short)0);
        return Random.Range(_settings.minShootDelay, _settings.maxShootDelay);
    }

    private float ShootPhase()
    {
        _tank.Shoot.ServerSetShooting(true);
        return Random.Range(_settings.minShootTime, _settings.maxShootTime);
    }

    public void Update(float deltaTime)
    {
        _scanner.Update(deltaTime);
        _timeUntilNextPhase -= deltaTime;
        if (_timeUntilNextPhase > 0 && !CheckPhaseInterrupted())
            return;
        if (DebugLogEnabled)
            PrintDebugLog(_currentPhase + (_timeUntilNextPhase > 0 ? " Phase Interrupted" : " Phase Finished"));
        ChooseNewPhase();
        _timeUntilNextPhase = ExecutePhase();
    }

    public void NotifyTeamChanged()
    {
        _scanner.SetTeam(_tank.Parameters.TeamId);
    }

}
