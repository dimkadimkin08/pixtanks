using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputDisplay : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private GameObject allDisplayContainer;

    [Header("Aim Line")]
    [SerializeField] private GameObject lineDisplay;
    [SerializeField] private SpriteRenderer lineSpriteRenderer;

    [Header("Drive Direction")]
    [SerializeField] private GameObject driveDirDisplay;
    [SerializeField] private Vector3 normalDirSize = Vector3.one;
    [SerializeField] private Vector3 drivingDirSize = Vector3.one;
    [SerializeField] private SpriteRenderer driveDirSpriteRenderer;

    [Header("Tank Privot")]
    [SerializeField] private GameObject[] playerPrivots;

    [Header("Cursor")]
    [SerializeField] private GameObject currentTankCursorPivot;
    [SerializeField] private GameObject cursorPivot;
    [SerializeField] private GameObject cursorDisplay;

    [Header("Drive Mode Indicators")]
    [SerializeField] private GameObject[] driveModeIndicators;
    [SerializeField] private GameObject[] backDriveModeIndicators;
    [SerializeField] private GameObject[] aimModeIndicators;
    [SerializeField] private GameObject[] relativeAimModeIndicators;

    [Header("Sounds")]
    [SerializeField] private AudioSource driveModeSwitchSound;
    [SerializeField] private AudioSource aimModeSwitchSound;

    [Header("Aim Match Detect")]
    [SerializeField] private float matchThreshold = 0.2f;
    [SerializeField] private Color matchColor;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color stopColor;

    private Camera _mainCamera;
    private bool _lastIsPlayer, _lastLineActive, _lastDriveDirActive, _lastCursorActive;
    private ushort _lastDriveMode;
    private Vector2 _lastDriveDir = Vector2.up;

    private void OnEnable()
    {
        _mainCamera = Camera.main;
        UpdateAimDisplay(true);
    }

    private void Update() => UpdateAimDisplay();

    private void UpdateAimDisplay(bool force = false)
    {
        var playerChanged = false;
        var player = NetworkTank.LocalPlayer;
        var hasPlayer = player != null;

        if (force || _lastIsPlayer != hasPlayer)
        {
            playerChanged = true;
            if (allDisplayContainer)
                allDisplayContainer.SetActive(hasPlayer);
            _lastIsPlayer = hasPlayer;
        }

        if (!hasPlayer && !force && !playerChanged) return;

        var input = NetworkPlayerInputsHandler.PlayerCurrentInput;

        UpdateDisplay(driveDirDisplay, ref _lastDriveDirActive,
            hasPlayer && input.DriveMode != NetworkPlayerInputsHandler.AIM_AND_RELATIVE_DRIVE_MODE,
            () => UpdateDriveDir(player.transform, input),
            force: force || playerChanged);
        UpdateDisplay(lineDisplay, ref _lastLineActive,
            hasPlayer && player.ShowDirectionLine
            && input.DriveMode != NetworkPlayerInputsHandler.WASD_DRIVE_MODE
            && input.DriveMode != NetworkPlayerInputsHandler.WASD_BACK_DRIVE_MODE,
            () => UpdateLine(player.transform),
            force: force || playerChanged);
        UpdateDisplay(cursorDisplay, ref _lastCursorActive, hasPlayer && player.UsesCursor, force: force || playerChanged);

        if (cursorPivot)
        {
            cursorPivot.transform.position = _mainCamera.ScreenToWorldPoint(Mouse.current.position.value);
            if (!NetworkTank.LocalPlayer)
            {
                currentTankCursorPivot.transform.position = cursorPivot.transform.position;
            }
            else
            {
                var targetPos = NetworkTank.LocalPlayer.Shoot.CursorPos;

                float lerpTime = 0.015f;
                float t = Time.deltaTime / lerpTime;

                currentTankCursorPivot.transform.position = Vector3.Lerp(
                    currentTankCursorPivot.transform.position,
                    targetPos,
                    t
                );
            }
        }

        if (hasPlayer)
        {
            Vector2 playerPos = player.transform.position;
            foreach (var obj in playerPrivots)
                obj.transform.position = playerPos;
        }


        if (force || playerChanged || _lastDriveMode != input.DriveMode)
        {
            _lastDriveMode = input.DriveMode;
            SetDriveModeUI(input.DriveMode);
        }
    }

    private void UpdateDisplay(GameObject display, ref bool lastState, bool currentState, System.Action onActive = null, bool force = false)
    {
        if (!display) return;
        if (lastState == currentState && !force && onActive == null) return;

        if (lastState != currentState || force)
        {
            display.SetActive(currentState);
            lastState = currentState;
        }

        if (currentState)
            onActive?.Invoke();
    }

    private void UpdateDriveDir(Transform player, NetworkPlayerInputsHandler.PlayerInputMessage input)
    {
        var isMoving = input.DriveInput != Vector2.zero;
        var dir = isMoving ? input.DriveInput : _lastDriveDir;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        driveDirDisplay.transform.SetPositionAndRotation(player.position, Quaternion.Euler(0, 0, angle));

        bool isMatch = false;
        switch(input.DriveMode)
        {
            case NetworkPlayerInputsHandler.AIM_AND_WASD_DRIVE_MODE:
                isMatch = DriveAimDirectionsMatch(dir,
                    ((Vector2)_mainCamera.ScreenToWorldPoint(Mouse.current.position.value) - (Vector2)player.position).normalized);
                break;
            case NetworkPlayerInputsHandler.WASD_BACK_DRIVE_MODE:
                if (100 > Mathf.Abs(Mathf.DeltaAngle(angle, player.eulerAngles.z)))
                    isMatch = Mathf.Abs(Mathf.DeltaAngle(player.eulerAngles.z, angle)) < matchThreshold;
                else
                    isMatch = Mathf.Abs(Mathf.DeltaAngle(player.eulerAngles.z, angle) - 180) < matchThreshold;
                break;
            case NetworkPlayerInputsHandler.WASD_DRIVE_MODE:
                isMatch = Mathf.Abs(Mathf.DeltaAngle(player.eulerAngles.z, angle)) < matchThreshold;
                break;
        }

        if (driveDirSpriteRenderer)
            driveDirSpriteRenderer.color = isMoving ? (isMatch ? matchColor : defaultColor) : stopColor;

        driveDirDisplay.transform.localScale = isMoving ? drivingDirSize : normalDirSize;
        _lastDriveDir = dir;
    }

    private void UpdateLine(Transform player)
    {
        Vector2 origin = NetworkTank.LocalPlayer
                && (Mouse.current != null && Mouse.current.rightButton.isPressed)
                ? NetworkTank.LocalPlayer.transform.position
                : _mainCamera.transform.position;

        Vector3 direction = NetworkPlayerInputsHandler.PlayerCurrentInput.CursorPos - origin;
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        lineDisplay.transform.SetPositionAndRotation(
            player.position,
            Quaternion.Euler(0, 0, angle)
        );

        var isMatch = Mathf.Abs(Mathf.DeltaAngle(player.eulerAngles.z, angle)) < matchThreshold;
        if (lineSpriteRenderer)
            lineSpriteRenderer.color = isMatch ? matchColor : defaultColor;
    }

    private bool DriveAimDirectionsMatch(Vector2 a, Vector2 b) =>
        !((a.y != 0 && Mathf.Sign(a.y) != Mathf.Sign(b.y)) || (a.x != 0 && Mathf.Sign(a.x) != Mathf.Sign(b.x)));

    private void SetDriveModeUI(ushort driveMode)
    {
        foreach(var obj in driveModeIndicators)
            if (obj)
                obj.SetActive(driveMode == NetworkPlayerInputsHandler.WASD_DRIVE_MODE);

        foreach (var obj in backDriveModeIndicators)
            if (obj)
                obj.SetActive(driveMode == NetworkPlayerInputsHandler.WASD_BACK_DRIVE_MODE);

        foreach (var obj in aimModeIndicators)
            if (obj)
                obj.SetActive(driveMode == NetworkPlayerInputsHandler.AIM_AND_WASD_DRIVE_MODE);

        foreach (var obj in relativeAimModeIndicators)
            if (obj)
                obj.SetActive(driveMode == NetworkPlayerInputsHandler.AIM_AND_RELATIVE_DRIVE_MODE);

        if (driveMode != NetworkPlayerInputsHandler.WASD_DRIVE_MODE && driveMode != NetworkPlayerInputsHandler.WASD_BACK_DRIVE_MODE)
        {
            if (aimModeSwitchSound && aimModeSwitchSound.enabled && aimModeSwitchSound.gameObject.activeInHierarchy)
                aimModeSwitchSound.Play();
        }
        else
        {
            if (driveModeSwitchSound && driveModeSwitchSound.enabled && aimModeSwitchSound.gameObject.activeInHierarchy)
                driveModeSwitchSound.Play();
        }
    }
}
