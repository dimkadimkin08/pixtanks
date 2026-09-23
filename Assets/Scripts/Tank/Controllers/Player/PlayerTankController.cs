using UnityEngine;
using UnityEngine.EventSystems;
public class PlayerTankController
{
    private readonly NetworkTank _tank;

    public bool IsEnabled { get; private set; }

    public PlayerTankController(NetworkTank tank)
    {
        _tank = tank;
    }

    private NetworkPlayerInputsHandler.PlayerInputMessage lastInput;

    private void OnPlayerInput(int connectionId, NetworkPlayerInputsHandler.PlayerInputMessage input)
    {
        try
        {
            if (!_tank)
            {
                Disable();
                return;
            }
            if (connectionId == _tank.connectionToClient.connectionId)
            {
                lastInput = input;
                HandleCurrentPlayerInput(input);
            }
        }
        catch
        {
            Disable();
        }
    }

    private void HandleCurrentPlayerInput(NetworkPlayerInputsHandler.PlayerInputMessage input)
    {
        float angle = input.Angle + 90;
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        switch (input.DriveMode)
        {
            case NetworkPlayerInputsHandler.AIM_AND_RELATIVE_DRIVE_MODE:
                _tank.Movement.ServerSetDirection(dir);
                _tank.Movement.ServerSetDrive((short)System.Math.Sign(input.DriveInput.y));
                _tank.Movement.ServerSetSideDrive(0);
                break;
            case NetworkPlayerInputsHandler.AIM_AND_WASD_DRIVE_MODE:
                _tank.Movement.ServerSetDirection(dir);
                if (input.DriveInput != Vector2.zero)
                    _tank.Movement.ServerSetDrive(
                        (short)((input.DriveInput.y != 0 && Mathf.Sign(input.DriveInput.y) != Mathf.Sign(dir.y))
                        || (input.DriveInput.x != 0 && Mathf.Sign(input.DriveInput.x) != Mathf.Sign(dir.x))
                        ? -1
                        : 1)
                    );
                else
                    _tank.Movement.ServerSetDrive(0);
                _tank.Movement.ServerSetSideDrive(0);
                break;
            case NetworkPlayerInputsHandler.WASD_DRIVE_MODE:
            case NetworkPlayerInputsHandler.WASD_BACK_DRIVE_MODE:
            default:
                var driveActive = input.DriveInput != Vector2.zero;
                var angleDiff = Vector2.SignedAngle(dir, _tank.transform.up);
                if (input.DriveMode != NetworkPlayerInputsHandler.WASD_BACK_DRIVE_MODE || angleDiff < 100)
                {
                    _tank.Movement.ServerSetDirection(driveActive ? input.DriveInput.normalized : _tank.transform.up);
                    _tank.Movement.ServerSetDrive((short)(driveActive ? 1 : 0));
                }
                else
                {
                    _tank.Movement.ServerSetDirection(driveActive ? -input.DriveInput.normalized : _tank.transform.up);
                    _tank.Movement.ServerSetDrive((short)(driveActive ? -1 : 0));
                }
                _tank.Movement.ServerSetSideDrive(0);
                break;
        }
        _tank.Shoot.ServerSetShooting(input.ShootInput);
        _tank.Shoot.ServerSetCursorPos(input.CursorPos);
        _tank.Special.ServerSetSpecialPressed(input.SpecialInput);
    }

    public void Enable()
    {
        IsEnabled = true;
        GameNetworkManager.Singleton.PlayerInputsHandler.ServerPlayerInputListeners += OnPlayerInput;
        if (_tank && _tank.connectionToClient != null && !_tank.Death.IsDead
            && GameNetworkManager.Singleton.PlayerInputsHandler.ServerPlayerInputs.TryGetValue(_tank.connectionToClient.connectionId, out var input))
        {
            lastInput = input;
            HandleCurrentPlayerInput(input);
        }
    }

    public void Update()
    {
        if (IsEnabled && _tank && _tank.connectionToClient != null && !_tank.Death.IsDead)
        {
            HandleCurrentPlayerInput(lastInput);
        }
    }

    public void Disable()
    {
        GameNetworkManager.Singleton.PlayerInputsHandler.ServerPlayerInputListeners -= OnPlayerInput;
        IsEnabled = false;
    }
}
