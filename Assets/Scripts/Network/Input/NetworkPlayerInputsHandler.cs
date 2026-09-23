using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
public class NetworkPlayerInputsHandler
{

    #region Server

    public event Action<int, PlayerInputMessage> ServerPlayerInputListeners;
    public readonly Dictionary<int, PlayerInputMessage> ServerPlayerInputs = new();

    public void OnStartServer()
    {
        NetworkServer.RegisterHandler<PlayerInputMessage>(ServerOnPlayerInput);
    }

    public void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        ServerPlayerInputs.Remove(conn.connectionId);
    }

    public void OnStopServer()
    {
        ServerPlayerInputListeners = null;
        ServerPlayerInputs.Clear();
        NetworkServer.UnregisterHandler<PlayerInputMessage>();
    }

    private void ServerOnPlayerInput(NetworkConnectionToClient conn, PlayerInputMessage inputMessage)
    {
        if (ServerPlayerInputs.TryGetValue(conn.connectionId, out var oldValue) && oldValue.Equals(inputMessage))
            return;
        ServerPlayerInputs[conn.connectionId] = inputMessage;
        ServerPlayerInputListeners?.Invoke(conn.connectionId, inputMessage);
    }

    public const ushort WASD_DRIVE_MODE = 0;
    public const ushort WASD_BACK_DRIVE_MODE = 1;
    public const ushort AIM_AND_WASD_DRIVE_MODE = 2;
    public const ushort AIM_AND_RELATIVE_DRIVE_MODE = 3;
    public struct PlayerInputMessage : NetworkMessage
    {
        public Vector2 CursorPos;
        public float Angle;
        public Vector2 DriveInput;
        public ushort DriveMode;
        public bool ShootInput;
        public bool SpecialInput;
        public readonly bool Equals(PlayerInputMessage other) =>
            0.1f > Vector2.Distance(CursorPos, other.CursorPos)
            && 0.1f > Vector2.Distance(DriveInput, other.DriveInput)
            && 0.1f > Mathf.Abs(Mathf.Abs(Angle) - Mathf.Abs(other.Angle))
            && DriveMode == other.DriveMode
            && ShootInput == other.ShootInput
            && SpecialInput == other.SpecialInput;
    }

    #endregion

    #region Client

    public static bool AltControlsEnabled;
    public static PlayerInputMessage PlayerCurrentInput;

    public InputSystem_Actions clientInputActions;
    private PlayerInputMessage _clientCurrentInput;
    private PlayerInputMessage CurrentClientInput
    {
        get => _clientCurrentInput;
        set
        {
            PlayerCurrentInput = value;
            _clientCurrentInput = value;
        }
    }
    private Camera _mainCamera;
    private CameraMovement _mainCameraMovement;
    private float _sendTimeLeft;
    private bool _shootWasPressed;
    private bool _specialWasPressed;
    private float _lastAngle;
    private Vector2 _lastCursorPos;
    private bool _wasdDriveEnabled;
    private bool _wasdBackDriveEnabled;

    public void OnClientReady()
    {
        clientInputActions ??= new InputSystem_Actions();
        clientInputActions.Enable();
        clientInputActions.Player.RelativeAimMode.performed += ClientSwitchWasdDriveEnabled;
        clientInputActions.Player.BackDriveMode.performed += ClientSwitchWasdBackDriveEnabled;
        PauseMenuSwitch.OnPauseSwitch += ClientOnPauseActiveChanged;
        ChatInputUI.OnInputFocusSwitch += ClientOnChatInputFocusChanged;
        _mainCamera = Camera.main;
        if (_mainCamera)
            _mainCamera.TryGetComponent(out _mainCameraMovement);
    }

    public void OnClientUpdate()
    {
        if (clientInputActions != null && clientInputActions.Player.Shoot.IsPressed())
            _shootWasPressed = true;
        if (clientInputActions != null && clientInputActions.Player.Special.IsPressed())
            _specialWasPressed = true;
        if (_sendTimeLeft <= 0)
        {
            if (!PauseMenuSwitch.PauseActive && !ChatInputUI.IsInputVisible && !LobbyMenuUI.IsMenuVisible && !ShopMenuVisibilityController.IsVisible)
                ClientSendCurrentInput();
            else
                ClientSendNullInput();
            _shootWasPressed = false;
            _specialWasPressed = false;
            _sendTimeLeft = 1 / NetworkManager.singleton.sendRate;
        }
        else
        {
            _sendTimeLeft -= Time.deltaTime;
        }
    }

    public void OnStopClient()
    {
        clientInputActions?.Disable();
        if (clientInputActions != null)
        {
            clientInputActions.Player.RelativeAimMode.performed -= ClientSwitchWasdDriveEnabled;
            clientInputActions.Player.BackDriveMode.performed -= ClientSwitchWasdBackDriveEnabled;
        }
        PauseMenuSwitch.OnPauseSwitch -= ClientOnPauseActiveChanged;
        ChatInputUI.OnInputFocusSwitch -= ClientOnChatInputFocusChanged;
    }

    private void ClientSwitchWasdDriveEnabled(InputAction.CallbackContext callbackContext)
    {
        if (!PauseMenuSwitch.PauseActive && !ChatInputUI.IsInputVisible && !LobbyMenuUI.IsMenuVisible)
            _wasdDriveEnabled = !_wasdDriveEnabled;
    }

    private void ClientSwitchWasdBackDriveEnabled(InputAction.CallbackContext callbackContext)
    {
        if (!PauseMenuSwitch.PauseActive && !ChatInputUI.IsInputVisible && !LobbyMenuUI.IsMenuVisible)
            _wasdBackDriveEnabled = !_wasdBackDriveEnabled;
    }

    private void ClientOnPauseActiveChanged(bool pauseActive)
    {
        if (ChatInputUI.IsInputVisible || LobbyMenuUI.IsMenuVisible || pauseActive)
            ClientSendNullInput();
        else
            ClientSendCurrentInput();
    }

    private void ClientOnChatInputFocusChanged(bool chatInputFocus)
    {
        if (chatInputFocus || PauseMenuSwitch.PauseActive)
            ClientSendNullInput();
        else
            ClientSendCurrentInput();
    }

    private void ClientSendNullInput()
    {
        var input = CurrentClientInput;
        input.DriveInput = Vector2.zero;
        input.ShootInput = false;
        if (!CurrentClientInput.Equals(input))
        {
            CurrentClientInput = input;
            NetworkClient.Send(CurrentClientInput);
        }
    }

    private void ClientSendCurrentInput()
    {
        if (clientInputActions?.Player == null)
            return;
        if (!_mainCamera)
            _mainCamera = Camera.main;
        
        float angle = _lastAngle;
        Vector3 cursorPos = _lastCursorPos;
        if (_mainCamera && !PauseMenuSwitch.PauseActive && !ChatInputUI.IsInputVisible && !LobbyMenuUI.IsMenuVisible)
        {
            cursorPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.value);
            Vector3 origin = NetworkTank.LocalPlayer
                && (Mouse.current != null && Mouse.current.rightButton.isPressed)
                ? NetworkTank.LocalPlayer.transform.position
                : _mainCamera.transform.position;
            
            Vector3 dir = cursorPos - origin;
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        }

        var input = new PlayerInputMessage
        {
            CursorPos = cursorPos,
            Angle = angle,
            DriveInput = clientInputActions.Player.Move.ReadValue<Vector2>(),
            ShootInput = _shootWasPressed || clientInputActions.Player.Shoot.IsPressed(),
            SpecialInput = _specialWasPressed || clientInputActions.Player.Special.IsPressed(),
            DriveMode = AltControlsEnabled && _wasdDriveEnabled
                ? (_wasdBackDriveEnabled ? WASD_BACK_DRIVE_MODE : WASD_DRIVE_MODE)
                : AIM_AND_RELATIVE_DRIVE_MODE
        };
        _shootWasPressed = false;
        if (!CurrentClientInput.Equals(input))
        {
            CurrentClientInput = input;
            if (NetworkClient.active)
                NetworkClient.Send(CurrentClientInput);
        }
        _lastAngle = angle;
        _lastCursorPos = cursorPos;
    }

    #endregion

}
