using Mirror;
using System.Linq;
using UnityEngine;
public class TankDrone : NetworkBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Drone Body")]
    [SerializeField]
    private GameObject droneBody;
    [SerializeField]
    private Rigidbody2D droneBodyRigidbody;

    [Header("Target Point")]
    [SerializeField]
    private Transform targetPoint;
    [SerializeField, Tooltip("Use input cursor as target instead of targetPoint")]
    private bool useCursorAsTarget;
    [SerializeField]
    private float targetMinDistance = 0.5f;

    [Header("Aim")]
    [SerializeField]
    private AimMode aimMode;

    [Header("Movement")]
    [SerializeField]
    private float maxRangeFromTank = 4;
    [SerializeField]
    private float teleportRangeFromTank = 7;
    [SerializeField]
    private float lerpSpeed = 1.5f;
    [SerializeField]
    private float minSpeed = 5;
    [SerializeField]
    private float maxSpeed = 15;
    [SerializeField]
    private int[] shootIndexes;
    [SerializeField]
    private float speedDuringShoot = 1;
    [SerializeField]
    private bool rotateOnShoot = true;

    [Header("Sound")]
    [SerializeField]
    private AudioSource shootSound;
    [SerializeField]
    private float shootSoundDelay;

    [Header("Animation")]
    [SerializeField]
    private SimpleSpriteAnimation shootAnimation;

    [SyncVar]
    private Vector2 dronePosition;
    [SyncVar]
    private float droneRotation;

    private enum AimMode { TankDirection, TargetPoint }

    public override void OnStartServer()
    {
        dronePosition = transform.position;
        if (droneBodyRigidbody)
            droneBodyRigidbody.bodyType = RigidbodyType2D.Dynamic;
    }

    public override void OnStartClient()
    {
        tank.Shoot.OnShoot += ClientOnShoot;
        if (droneBodyRigidbody)
            droneBodyRigidbody.bodyType = isHost ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
    }

    public override void OnStopClient()
    {
        tank.Shoot.OnShoot -= ClientOnShoot;
    }

    private void Awake()
    {
        droneBody.transform.parent = null;
        tank.Death.OnDeath += () => Destroy(droneBody);
        droneBody.SetActive(gameObject.activeInHierarchy);
    }

    private void OnDestroy()
    {
        if (droneBody)
            Destroy(droneBody);
    }

    private void OnEnable()
    {
        if (droneBody)
            droneBody.SetActive(true);
        if (isServer)
        {
            if (droneBodyRigidbody)
            {
                dronePosition = droneBodyRigidbody.position;
                droneRotation = droneBodyRigidbody.rotation;
            }
        }
        else
        {
            droneBodyRigidbody.position = dronePosition;
            droneBodyRigidbody.rotation = droneRotation;
        }
    }

    private void OnDisable()
    {
        if (droneBody)
            droneBody.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        Vector2 targetPos = useCursorAsTarget
            ? tank.Shoot.ServerGetCursorPos()
            : targetPoint.position;

        Vector2 lookAt = targetPos;

        float newRotation = droneBodyRigidbody.rotation;
        var isShooting = tank.Shoot.IsShooting && shootIndexes.Contains(tank.Shoot.PatternIndex);
        if (!isShooting || rotateOnShoot)
        {
            switch (aimMode)
            {
                case AimMode.TargetPoint:
                    Vector2 lookDir = (lookAt - droneBodyRigidbody.position).normalized;
                    newRotation = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
                    break;
                case AimMode.TankDirection:
                    newRotation = tank.Rigidbody.rotation;
                    break;
            }
        }



        Vector2 currentPos = droneBodyRigidbody.position;
        Vector2 toCursor = targetPos - currentPos;
        float distToCursor = toCursor.magnitude;

        Vector2 moveTargetPos;

        if (distToCursor < targetMinDistance)
        {
            float angleRad = (droneBodyRigidbody.rotation + 90f) * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            moveTargetPos = currentPos - forward * (targetMinDistance - distToCursor);
        }
        else
        {
            moveTargetPos = Vector2.MoveTowards(targetPos, currentPos, targetMinDistance);
        }

        Vector2 positionInRange = Vector2.MoveTowards(tank.Rigidbody.position, moveTargetPos, maxRangeFromTank);
        Vector2 toTarget = positionInRange - droneBodyRigidbody.position;
        float dist = toTarget.magnitude;
        float moveSpeed = (isShooting ? speedDuringShoot : 1) * Mathf.Clamp(dist * lerpSpeed, minSpeed, maxSpeed);
        Vector2 move = moveSpeed * toTarget.normalized;

        droneBodyRigidbody.linearVelocity = move.magnitude > minSpeed / 2 ? move : Vector2.zero;
        droneBodyRigidbody.rotation = newRotation;

        if (Vector2.Distance(droneBodyRigidbody.position, tank.Rigidbody.position) > teleportRangeFromTank)
            droneBody.transform.position = tank.Rigidbody.position;
    }

    private void Update()
    {
        if (isServer)
        {
            dronePosition = droneBodyRigidbody.position;
            droneRotation = droneBodyRigidbody.rotation;
        }
        else
        {
            var newPosition = Vector2.Lerp(droneBody.transform.position, dronePosition, 10 * Time.deltaTime);
            droneBody.transform.SetPositionAndRotation(newPosition, Quaternion.Euler(0, 0, droneRotation));
        }
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, droneRotation);
    }

    private void ClientOnShoot(TankShoot.PatternData patternData)
    {
        if (shootAnimation)
            shootAnimation.Play();
        if (shootSound && shootSound.enabled)
            if (shootSoundDelay <= 0)
                shootSound.Play();
            else
                shootSound.PlayDelayed(shootSoundDelay);
    }

}