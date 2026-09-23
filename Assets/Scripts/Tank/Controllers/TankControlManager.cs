using JetBrains.Annotations;
using Mirror;
using UnityEngine;
public class TankControlManager : NetworkBehaviour
{

    [Header("Controller")]

    public TankController serverCurrentController = TankController.None;

    [Header("AI")]

    public TankAISettings tankAISettings;
    public TankAIScannerSettings scannerSettings;
    [SerializeField]
    private bool showAIDebugLog;

    [Header("Gun transform (Optional)")]

    public Transform gunCenterTransform;

    [Header("Tank")]

    [SerializeField]
    private NetworkTank tank;

    [CanBeNull]
    private PlayerTankController _playerController;
    [CanBeNull]
    private AITankController _aiTankController;

    public enum TankController { None, AI, Player }

    public override void OnStartServer()
    {
        tank.Parameters.OnTeamIdSet += OnTeamChanged;
    }

    public override void OnStopServer()
    {
        tank.Parameters.OnTeamIdSet -= OnTeamChanged;
        if (_playerController?.IsEnabled == true)
            _playerController.Disable();
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;
        switch (serverCurrentController)
        {
            case TankController.AI:
                _aiTankController ??= new AITankController(tank, tankAISettings, scannerSettings, gunCenterTransform);
                _aiTankController.DebugLogEnabled = showAIDebugLog;
                _aiTankController.Update(Time.fixedDeltaTime);
                if (_playerController?.IsEnabled == true)
                    _playerController.Disable();
                break;
            case TankController.Player:
                _playerController ??= new PlayerTankController(tank);
                if (!_playerController.IsEnabled)
                    _playerController.Enable();
                else
                    _playerController.Update();
                break;
            case TankController.None:
                if (_playerController?.IsEnabled == true)
                    _playerController.Disable();
                break;
        }
    }

    private void Update()
    {
        if (!isServer)
            return;
        switch (serverCurrentController)
        {
            case TankController.Player:
                if (_playerController?.IsEnabled == true)
                    _playerController.Update();
                break;
        }
    }

    private void OnTeamChanged(string old, string current)
    {
        _aiTankController?.NotifyTeamChanged();
    }
}
