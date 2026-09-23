using UnityEngine;
using UnityEngine.InputSystem;
public class CameraMovement : MonoBehaviour
{

    [SerializeField]
    private Transform target;
    [SerializeField]
    private bool handleLocalPlayer;
    [SerializeField]
    private Vector3 globalOffset = new(0, 0, -10);
    [SerializeField]
    private Vector2 localOffset = Vector2.up;
    [SerializeField]
    private Vector2 localOffsetMultiplier = new(0.5f, 2);
    [SerializeField]
    private Vector2 mouseOffsetMultiplier = new(0.5f, 1);
    [SerializeField]
    private float speed = 10;
    [SerializeField]
    private float mouseSpeedMultiplier = 1.5f;

    [HideInInspector]
    public Vector3 shakeOffset;

    private Vector3 position;

    public Transform Target => target;
    public Vector3 TargetPosWithOffset { get; private set; }

    private void OnEnable()
    {
        position = transform.position;
        LandingCapsule.OnLocalPlayerLandingCapsule += OnLocalPlayerLandingCapsule;
        NetworkTank.OnLocalPlayerSpawn += OnLocalPlayerSpawned;
    }

    private void OnDisable()
    {
        LandingCapsule.OnLocalPlayerLandingCapsule -= OnLocalPlayerLandingCapsule;
        NetworkTank.OnLocalPlayerSpawn -= OnLocalPlayerSpawned;
    }

    private void OnLocalPlayerLandingCapsule(LandingCapsule landingCapsule)
    {
        if (handleLocalPlayer)
            target = landingCapsule.transform;
    }

    private void OnLocalPlayerSpawned(NetworkTank localPlayerTank)
    {
        if (handleLocalPlayer)
            target = localPlayerTank.transform;
    }

    private void Update()
    {
        if (target)
        {
            var mouseOffset = GetMouseOffset01();
            var offset = globalOffset + (Vector3)((target.up * localOffset.y + target.right * localOffset.x) * localOffsetMultiplier);
            offset += (Vector3)(mouseOffsetMultiplier != Vector2.zero
                    ? mouseOffset * mouseOffsetMultiplier
                    : Vector2.zero);
            TargetPosWithOffset = target.position + offset;
            var finalSpeed = speed * (mouseOffset != Vector2.zero ? mouseSpeedMultiplier : 1f);
            position = Vector3.Lerp(position, TargetPosWithOffset, Time.deltaTime * finalSpeed);
        }
        transform.position = position + shakeOffset;
    }


    private Vector2 GetMouseOffset01()
    {
        var mouse = Mouse.current;
        if (mouse == null || !Mouse.current.rightButton.isPressed) return Vector2.zero;

        Vector2 pos = mouse.position.ReadValue();
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 delta = pos - center;

        delta *= 1.5f;

        Vector2 norm = new Vector2(
            Mathf.Clamp(delta.x / center.x, -1f, 1f),
            Mathf.Clamp(delta.y / center.y, -1f, 1f)
        );

        return norm;
    }
}
