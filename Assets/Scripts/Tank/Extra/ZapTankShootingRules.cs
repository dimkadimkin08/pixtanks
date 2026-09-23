using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class ZapTankShootingRules : TankShootingRules
{
    [SerializeField]
    private NetworkTank tank;

    [Space]

    [SerializeField]
    private ushort maxStacks = 6;
    [SerializeField]
    private ushort startStacks = 3;

    private string _teamId;
    private GameTanksManager.Team currentTeam;

    [SyncVar]
    private bool _isAbleToShoot;
    [SyncVar]
    private ushort _currentStacks;

    public override bool IsAbleToShoot => _isAbleToShoot;
    public ushort CurrentStacks => _currentStacks;

    private readonly List<NetworkTank> _serverTanksInRange = new();
    private readonly List<NetworkStructure> _serverStructuresInRange = new();

    public override void OnStartServer()
    {
        _currentStacks = startStacks;

        _teamId = tank.Parameters.TeamId;
        UpdateCurrentTeam();

        tank.Parameters.OnTeamIdSet += OnTeamIdChanged;
    }

    public override void OnStopServer()
    {
        if (tank && tank.Parameters != null)
            tank.Parameters.OnTeamIdSet -= OnTeamIdChanged;
    }

    private void OnTeamIdChanged(string old, string newValue)
    {
        _teamId = newValue;
        UpdateCurrentTeam();
    }

    private void UpdateCurrentTeam()
    {
        currentTeam = GameTanksManager.Singleton.GetTeam(_teamId);
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        if (!isServer)
            return;

        if (coll.isTrigger)
            return;

        if (!coll.attachedRigidbody || coll.attachedRigidbody == tank.Rigidbody)
            return;

        if (coll.attachedRigidbody.TryGetComponent(out NetworkTank otherTank))
        {
            if (!_serverTanksInRange.Contains(otherTank))
                _serverTanksInRange.Add(otherTank);
        }
        else if (coll.attachedRigidbody.TryGetComponent(out NetworkStructure structure))
        {
            if (!_serverStructuresInRange.Contains(structure))
                _serverStructuresInRange.Add(structure);
        }
    }

    private void OnTriggerExit2D(Collider2D coll)
    {
        if (!isServer)
            return;

        if (coll.isTrigger)
            return;

        if (!coll.attachedRigidbody || coll.attachedRigidbody == tank.Rigidbody)
            return;

        if (coll.attachedRigidbody.TryGetComponent(out NetworkTank otherTank))
        {
            _serverTanksInRange.Remove(otherTank);
        }
        else if (coll.attachedRigidbody.TryGetComponent(out NetworkStructure structure))
        {
            _serverStructuresInRange.Remove(structure);
        }
    }

    private void Update()
    {
        if (!isServer)
            return;

        _serverTanksInRange.RemoveAll(t => !t);
        _serverStructuresInRange.RemoveAll(s => !s);

        bool hasValidTankTarget = false;

        for (int i = 0; i < _serverTanksInRange.Count; i++)
        {
            var otherTank = _serverTanksInRange[i];
            var otherTeamId = otherTank.Parameters.TeamId;

            if (currentTeam.enemies.Contains(otherTeamId))
            {
                hasValidTankTarget = true;
                break;
            }

            if (_currentStacks > 0 && otherTeamId == _teamId && otherTank.Parameters.CurrentHp < otherTank.Parameters.MaxHp)
            {
                hasValidTankTarget = true;
                break;
            }
        }

        _isAbleToShoot = hasValidTankTarget || _serverStructuresInRange.Count > 0;
    }

    [Server]
    public void ServerConsumeStack()
    {
        if (_currentStacks > 0)
            _currentStacks = _currentStacks.Sub(1);
    }

    [Server]
    public void ServerRestoreStack()
    {
        if (_currentStacks < maxStacks)
            _currentStacks = _currentStacks.Add(1);
    }
}
