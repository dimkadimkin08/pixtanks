//using System.Linq;
//using UnityEngine;
//using Random = UnityEngine.Random;
//public class OldAITankController
//{
//    private static readonly Vector2[] IdleMoveDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

//    private readonly NetworkTank _tank;
//    private readonly TankAISettings _settings;
//    private readonly TankScanner _scanner;

//    private readonly RaycastHit2D[] _wallCheckBuffer = new RaycastHit2D[1];

//    private string DebugLogPrefix => $"TankAI (netId: {_tank.netId}): ";

//    private float _timeUntilNextPhase;
//    private Phase _currentPhase;

//    public bool DebugLogEnabled;

//    private enum Phase { Idle, IdleMove, BattleMoveDelay, BattleMove, BattleMoveBlind, PrepareShoot, Shoot, DodgeMove }

//    public OldAITankController(NetworkTank tank, TankAISettings settings, TankAIScannerSettings scannerSettings)
//    {
//        _tank = tank;
//        _settings = settings;
//        _scanner = new TankScanner(tank.Parameters.TeamId, tank.Rigidbody, scannerSettings);
//    }

//    private void PrintDebugLog(object message)
//    {
//        Debug.Log(DebugLogPrefix + message, _tank.gameObject);
//    }

//    private void ChooseNewPhase()
//    {
//        if (!_scanner.TargetEnemy)
//            _currentPhase = _currentPhase == Phase.Idle ? Phase.IdleMove : Phase.Idle;
//        else if (_currentPhase == Phase.PrepareShoot)
//            _currentPhase = Phase.Shoot;
//        else if (_currentPhase == Phase.Shoot && RandomUtils.Chance(_settings.dodgeChance))
//            _currentPhase = Phase.DodgeMove;
//        else if (CheckAbleToAttack())
//            _currentPhase = Phase.PrepareShoot;
//        else if (_currentPhase != Phase.BattleMoveDelay && RandomUtils.Chance(_settings.battleMoveDelayChance))
//            _currentPhase = Phase.BattleMoveDelay;
//        else
//            _currentPhase = RandomUtils.Chance(_settings.battleMoveInterruptChance) ? Phase.BattleMove : Phase.BattleMoveBlind;
//        if (DebugLogEnabled)
//            PrintDebugLog($" Phase: {_currentPhase}");
//    }

//    private bool CheckAbleToAttack()
//    {
//        if (!_scanner.TargetEnemy)
//            return false;
//        var vectorToTarget = (Vector2)_scanner.TargetEnemy.position - _tank.Rigidbody.position;
//        var lowerAxis = Mathf.Min(Mathf.Abs(vectorToTarget.x), Mathf.Abs(vectorToTarget.y));
//        var higherAxis = Mathf.Max(Mathf.Abs(vectorToTarget.x), Mathf.Abs(vectorToTarget.y));
//        var result = lowerAxis < _settings.maxShootDeviation && higherAxis < _settings.maxShootDistance;
//        return result;
//    }

//    private bool CheckPhaseInterrupted()
//    {
//        return _currentPhase switch
//        {
//            Phase.Idle => _scanner.TargetEnemy,
//            Phase.IdleMove => !_tank.Movement.IsMoving || _scanner.TargetEnemy,
//            Phase.BattleMoveDelay => CheckAbleToAttack(),
//            Phase.BattleMove => !_tank.Movement.IsMoving || CheckAbleToAttack(),
//            Phase.BattleMoveBlind => !_tank.Movement.IsMoving,
//            Phase.DodgeMove => !_tank.Movement.IsMoving,
//            _ => false
//        };
//    }

//    private float ExecutePhase()
//    {
//        return _currentPhase switch
//        {
//            Phase.Idle => IdlePhase(),
//            Phase.IdleMove => IdleMovePhase(),
//            Phase.BattleMoveDelay => BattleMoveDelayPhase(),
//            Phase.BattleMove or Phase.BattleMoveBlind => BattleMovePhase(),
//            Phase.PrepareShoot => PrepareShootPhase(),
//            Phase.Shoot => ShootPhase(),
//            Phase.DodgeMove => DodgeMovePhase(),
//            _ => 1
//        };
//    }

//    private bool CheckFreePath(Vector2 direction, float distance)
//    {
//        _tank.Rigidbody.Cast(direction, _wallCheckBuffer, distance);
//        return !_wallCheckBuffer[0];
//    }

//    private void SetFreeMoveDirection(float wallCheck, params Vector2[] directions)
//    {
//        var freeDirections = directions.Where(direction => CheckFreePath(direction, wallCheck)).ToArray();
//        _tank.Movement.ServerSetMoveDirection(RandomUtils.Between(freeDirections.Length > 0 ? freeDirections : directions));
//    }

//    private float IdlePhase()
//    {
//        _tank.Shoot.ServerSetShooting(false);
//        _tank.Movement.ServerSetInput(TankMovement.MoveDirection.None);
//        return Random.Range(_settings.minIdleMoveDelay, _settings.maxIdleMoveDelay);
//    }

//    private float IdleMovePhase()
//    {
//        _tank.Shoot.ServerSetShooting(false);
//        SetFreeMoveDirection(_settings.idleMoveWallCheck, IdleMoveDirections);
//        return Random.Range(_settings.minIdleMoveTime, _settings.maxIdleMoveTime);
//    }

//    private float BattleMoveDelayPhase()
//    {
//        _tank.Shoot.ServerSetShooting(false);
//        _tank.Movement.ServerSetMoveDirection(Vector2.zero);
//        return Random.Range(_settings.minBattleMoveDelay, _settings.maxBattleMoveDelay);
//    }

//    private float BattleMovePhase()
//    {
//        _tank.Shoot.ServerSetShooting(false);
//        var vectorToTarget = (Vector2)_scanner.TargetEnemy.position - _tank.Rigidbody.position;
//        if (_settings.maxShootDeviation > Mathf.Min(Mathf.Abs(vectorToTarget.x), Mathf.Abs(vectorToTarget.y)))
//            _tank.Movement.ServerSetMoveDirection(vectorToTarget);
//        else
//            SetFreeMoveDirection(_settings.battleMoveWallCheck, Vector2.right * vectorToTarget.x, Vector2.up * vectorToTarget.y);
//        return Random.Range(_settings.minBattleMoveTime, _settings.maxBattleMoveTime);
//    }

//    private float PrepareShootPhase()
//    {
//        _tank.Shoot.ServerSetShooting(false);
//        _tank.Movement.ServerSetMoveDirection((Vector2)_scanner.TargetEnemy.position - _tank.Rigidbody.position);
//        if (!RandomUtils.Chance(_settings.chanceToMoveDuringShoot))
//            _tank.Movement.ServerSetInput(TankMovement.MoveDirection.None);
//        return Random.Range(_settings.minShootDelay, _settings.maxShootDelay);
//    }

//    private float ShootPhase()
//    {
//        _tank.Shoot.ServerSetShooting(true);
//        return Random.Range(_settings.minShootTime, _settings.maxShootTime);
//    }

//    private float DodgeMovePhase()
//    {
//        _tank.Shoot.ServerSetShooting(false);
//        SetFreeMoveDirection(_settings.dodgeMoveWallCheck, _tank.Rigidbody.transform.right, -_tank.Rigidbody.transform.right);
//        return Random.Range(_settings.minDodgeMoveTime, _settings.maxDodgeMoveTime);
//    }

//    public void Update(float deltaTime)
//    {
//        _scanner.Update(deltaTime);
//        _timeUntilNextPhase -= deltaTime;
//        if (_timeUntilNextPhase > 0 && !CheckPhaseInterrupted())
//            return;
//        if (DebugLogEnabled)
//            PrintDebugLog(_currentPhase + (_timeUntilNextPhase > 0 ? " Phase Interrupted" : " Phase Finished"));
//        ChooseNewPhase();
//        _timeUntilNextPhase = ExecutePhase();
//    }

//    public void NotifyTeamChanged()
//    {
//        _scanner.SetTeam(_tank.Parameters.TeamId);
//    }

//}
