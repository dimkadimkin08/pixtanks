//using Mirror;
//using UnityEngine;
//public class OldTankMovement : NetworkBehaviour
//{
//    public static MoveDirection VectorToMoveDirection(Vector2 vector)
//    {
//        if (vector == Vector2.zero)
//            return MoveDirection.None;
//        if (Mathf.Abs(vector.x) > Mathf.Abs(vector.y))
//            return vector.x > 0 ? MoveDirection.Right : MoveDirection.Left;
//        return vector.y > 0 ? MoveDirection.Up : MoveDirection.Down;
//    }

//    [Header("Tank")]

//    [SerializeField]
//    private NetworkTank tank;

//    [Header("Movement")]

//    [SerializeField]
//    private float baseMoveSpeed = 5;

//    [Header("Force Physics")]
//    [SerializeField]
//    private float forceDeceleration = 1;

//    [Header("Behaviour during shooting")]

//    [SerializeField]
//    private float speedDuringShoot = 0.5f;
//    [SerializeField]
//    private bool rotateDuringShoot = true;
//    [SerializeField]
//    private float speedDuringReload = 0.75f;
//    [SerializeField]
//    private bool rotateDuringReload = true;

//    [SyncVar]
//    private Vector2Int _direction;
//    [SyncVar]
//    private ushort _rotation;

//    private Vector2 _force;

//    private Vector2 _lastPosition;

//    private float _timeUntilMoveCheck;

//    [Tooltip("The tank will stop moving and rotating if value > 0. This is used by other scripts to force freeze movement (the value isn't syncing to clients)")]
//    public int serverFreezeMovement = 0;

//    public ushort CurrentRotation => _rotation;

//    public Vector2Int CurrentDirection => _direction;

//    public float BaseMoveSpeed => baseMoveSpeed;

//    public bool IsMoving { get; private set; }

//    public enum MoveDirection { None, Up, Down, Left, Right }

//    public override void OnStartServer()
//    {
//        var currentTeam = GameTanksManager.Singleton.GetTeam(tank.Parameters.TeamId);
//        if (currentTeam != null)
//            tank.Rigidbody.includeLayers = currentTeam.includeObstacles;
//        tank.Parameters.OnTeamIdSet += ServerOnTankTeamChanged;
//    }

//    public override void OnStopServer()
//    {
//        tank.Parameters.OnTeamIdSet -= ServerOnTankTeamChanged;
//    }

//    private void FixedUpdate()
//    {
//        if (!isServer)
//            return;
//        var speed = GetSpeed();
//        if (IsAbleToRotate())
//        {
//            tank.Rigidbody.linearVelocity = (Vector2)_direction * speed;
//            tank.Rigidbody.rotation = _rotation;
//        }
//        else
//        {
//            var directionAvailable = 0.1 > Vector2.Distance(transform.up, _direction);
//            tank.Rigidbody.linearVelocity = directionAvailable ? transform.up * speed : Vector2.zero;
//        }
//        _force = Vector2.Lerp(_force, Vector2.zero, Time.fixedDeltaTime * 0.7f * forceDeceleration);
//        tank.Rigidbody.linearVelocity += _force;
//    }

//    private void Update()
//    {
//        if (IsMoving || _direction != Vector2Int.zero)
//            _timeUntilMoveCheck -= Time.deltaTime;
//        if (_timeUntilMoveCheck > 0)
//            return;
//        IsMoving = Vector2.Distance(tank.Rigidbody.position, _lastPosition) > 0.1f;
//        _lastPosition = tank.Rigidbody.position;
//        _timeUntilMoveCheck = 0.05f;
//    }

//    [Server]
//    private void ServerOnTankTeamChanged(string oldTeamId, string currentTeamId)
//    {
//        var currentTeam = GameTanksManager.Singleton.GetTeam(currentTeamId);
//        if (currentTeam != null)
//            tank.Rigidbody.includeLayers = currentTeam.includeObstacles;
//    }

//    private bool IsAbleToRotate()
//    {
//        if (serverFreezeMovement > 0)
//            return false;
//        if (!tank.Shoot)
//            return true;
//        return (rotateDuringShoot || !tank.Shoot.IsShooting) && (rotateDuringReload || !tank.Shoot.IsReloading);
//    }

//    private float GetSpeed()
//    {
//        if (serverFreezeMovement > 0)
//            return 0;

//        var speed = baseMoveSpeed;

//        if (tank.Parameters)
//            speed *= tank.Parameters.SpeedMultiplier;

//        if (!tank.Shoot)
//            return speed;

//        if (tank.Shoot.IsShooting)
//            speed *= speedDuringShoot;
//        else if (tank.Shoot.IsReloading)
//            speed *= speedDuringReload;

//        return Mathf.Max(speed, 0);
//    }

//    [Server]
//    public void ServerSetMoveDirection(MoveDirection moveDirection)
//    {
//        IsMoving = moveDirection != MoveDirection.None;
//        _direction = moveDirection switch
//        {
//            MoveDirection.Up => Vector2Int.up,
//            MoveDirection.Down => Vector2Int.down,
//            MoveDirection.Left => Vector2Int.left,
//            MoveDirection.Right => Vector2Int.right,
//            _ => Vector2Int.zero
//        };
//        _rotation = moveDirection switch
//        {
//            MoveDirection.Up => 0,
//            MoveDirection.Down => 180,
//            MoveDirection.Left => 90,
//            MoveDirection.Right => 270,
//            _ => _rotation
//        };
//    }

//    [Server]
//    public void ServerAddForce(Vector2 force)
//    {
//        _force += force / tank.Rigidbody.mass;
//    }

//    [Server]
//    public void ServerSetMoveDirection(Vector2 vector)
//    {
//        ServerSetMoveDirection(VectorToMoveDirection(vector));
//    }
//}
