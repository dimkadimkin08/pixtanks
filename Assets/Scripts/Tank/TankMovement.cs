using Mirror;
using UnityEngine;
using UnityEngine.UIElements;
public class TankMovement : NetworkBehaviour
{

    [Header("Tank")]

    [SerializeField]
    private NetworkTank tank;

    [Header("Movement")]

    [SerializeField]
    private float baseMoveSpeed = 5;
    [SerializeField]
    private float baseRotationSpeed = 10;
    [SerializeField]
    private float backMoveSpeedMultiplier = 1;
    //[SerializeField]
    //private float sideMoveSpeedMultiplier = 0.5f;

    [Header("Force Physics")]
    [SerializeField]
    private float baseForceDeceleration = 10;
    [SerializeField, Range(0, 1)]
    private float knockbackResistance = 0;

    [Header("Behaviour during shooting")]

    [SerializeField]
    private float speedDuringShoot = 0.5f;
    [SerializeField]
    private float rotateSpeedDuringShoot = 0.7f;
    [SerializeField]
    private float speedDuringReload = 0.75f;
    [SerializeField]
    private float rotateSpeedDuringReload = 0.9f;

    [Header("Extra")]
    [SerializeField]
    private int driveDuringShoot = 0;

    [SyncVar]
    private short _drive;

    private Vector2 _serverDirection;
    private short _serverSideDrive;

    private Vector2 _force;

    private Vector2 _lastPosition;
    private float _lastRotation;

    private float _timeUntilMoveCheck;
    private float _timeUntilRotationCheck;

    [HideInInspector]
    [Tooltip("The tank will stop moving and rotating if value > 0. This is used by other scripts to force freeze movement (the value isn't syncing to clients)")]
    public int serverFreezeMovement = 0;

    public float BaseMoveSpeed => baseMoveSpeed;

    public bool IsMoving { get; private set; }
    public bool IsRotating { get; private set; }

    public override void OnStartServer()
    {
        var currentTeam = GameTanksManager.Singleton.GetTeam(tank.Parameters.TeamId);
        if (currentTeam != null)
            tank.Rigidbody.includeLayers = currentTeam.includeObstacles;
        tank.Parameters.OnTeamIdSet += ServerOnTankTeamChanged;
    }

    public override void OnStopServer()
    {
        tank.Parameters.OnTeamIdSet -= ServerOnTankTeamChanged;
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        float prevAngle = tank.Rigidbody.rotation;
        if (_serverSideDrive != 0)
        {
            var rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0, 0, tank.Rigidbody.rotation - 45 * Mathf.Sign(_serverSideDrive)),
                10 * GetRotationSpeed() * Time.fixedDeltaTime
            );
            tank.Rigidbody.rotation = rotation.eulerAngles.z;
        }
        else if (_serverDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(_serverDirection.y, _serverDirection.x) * Mathf.Rad2Deg - 90f;

            var rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0, 0, angle),
                10 * GetRotationSpeed() * Time.fixedDeltaTime
            );

            tank.Rigidbody.rotation = rotation.eulerAngles.z;
        }
        var speed = GetSpeed();
        var driveY = driveDuringShoot == 0 || !tank.Shoot.IsShooting ? _drive : driveDuringShoot;
        Vector2 drive = new()
        {
            y = Mathf.Clamp(System.Math.Sign(driveY) * speed * (driveY < 0 ? backMoveSpeedMultiplier : 1), -speed * backMoveSpeedMultiplier, speed),
            x = 0
        };
        drive.y = Mathf.MoveTowards(drive.y, 0, Mathf.Abs(drive.x));
        tank.Rigidbody.linearVelocity = tank.transform.up * drive.y + tank.transform.right * drive.x;
        _force = Vector2.Lerp(_force, Vector2.zero, Time.fixedDeltaTime * baseForceDeceleration);
        tank.Rigidbody.linearVelocity += _force;
    }

    private void Update()
    {
        if (IsMoving || _drive != 0)
            _timeUntilMoveCheck -= Time.deltaTime;
        if (_timeUntilMoveCheck <= 0)
        {
            IsMoving = Vector2.Distance(tank.Rigidbody.position, _lastPosition) > 0.1f;
            _lastPosition = tank.Rigidbody.position;
            _timeUntilMoveCheck = 0.05f;
        }
        if (IsRotating || _serverDirection != Vector2.zero)
            _timeUntilRotationCheck -= Time.deltaTime;
        if (_timeUntilRotationCheck <= 0)
        {
            IsRotating = Mathf.Abs(Mathf.Abs(tank.Rigidbody.rotation) - Mathf.Abs(_lastRotation)) > 0.5f;
            _lastRotation = tank.Rigidbody.rotation;
            _timeUntilRotationCheck = 0.05f;
        }
    }

    [Server]
    private void ServerOnTankTeamChanged(string oldTeamId, string currentTeamId)
    {
        var currentTeam = GameTanksManager.Singleton.GetTeam(currentTeamId);
        if (currentTeam != null)
            tank.Rigidbody.includeLayers = currentTeam.includeObstacles;
    }

    private float GetSpeed()
    {
        if (serverFreezeMovement > 0)
            return 0;

        var speed = baseMoveSpeed;

        if (tank.Parameters)
            speed *= tank.Parameters.SpeedMultiplier;

        if (!tank.Shoot)
            return speed;

        if (tank.Shoot.IsShooting)
            speed *= speedDuringShoot;
        else if (tank.Shoot.IsReloading)
            speed *= speedDuringReload;

        return Mathf.Max(speed, 0);
    }

    private float GetRotationSpeed()
    {
        if (serverFreezeMovement > 0)
            return 0;

        var speed = baseRotationSpeed;

        if (tank.Parameters)
            speed *= tank.Parameters.SpeedMultiplier;

        if (!tank.Shoot)
            return speed;

        if (tank.Shoot.IsShooting)
            speed *= rotateSpeedDuringShoot;
        else if (tank.Shoot.IsReloading)
            speed *= rotateSpeedDuringReload;

        return Mathf.Max(speed, 0);
    }

    [Server]
    public void ServerSetDirection(Vector2 direction)
    {
        _serverDirection = direction.normalized;
    }

    [Server]
    public void ServerSetDrive(short drive)
    {
        _drive = drive;
    }

    [Server]
    public void ServerSetSideDrive(short sideDrive)
    {
        _serverSideDrive = sideDrive;
    }

    [Server]
    public void ServerAddForce(Vector2 force, bool bypassResistance = false)
    {
        if (bypassResistance)
            _force += force;
        else
            _force += force * (1 - knockbackResistance);
    }
}
