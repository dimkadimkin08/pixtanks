using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

public class TankAutoAttack : NetworkBehaviour
{
    [SerializeField] private NetworkTank _tank;
    [SerializeField] private ushort _baseDamage = 4;
    [SerializeField] protected string[] statusEffectResources;
    [SerializeField] private float _baseInterval = 0.4f;
    [SerializeField] private float _circleRadius = 0.75f;
    [SerializeField] private Vector2 _circleOffset = new(0, 0.75f);
    [SerializeField] private UnityEvent _clientOnHit;

    private float _serverTimer;

    private readonly HashSet<string> _enemyTeams = new();

    public override void OnStartServer()
    {

        UpdateEnemyTeams("", _tank.Parameters.TeamId);
        _tank.Parameters.OnTeamIdSet += UpdateEnemyTeams;
    }

    public override void OnStopServer()
    {
        _tank.Parameters.OnTeamIdSet -= UpdateEnemyTeams;
    }

    private void UpdateEnemyTeams(string old, string teamId)
    {
        _enemyTeams.Clear();

        var team = GameTanksManager.Singleton.GetTeam(teamId);
        if (team == null) return;

        foreach (var enemyId in team.enemies)
        {
            _enemyTeams.Add(enemyId);
        }
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;
        _serverTimer -= Time.fixedDeltaTime * _tank.Parameters.ReloadMultiplier;
        if (_serverTimer <= 0)
        {
            if (TryHit() && !_tank.Death.IsDead)
            {
                RpcOnHit();
                if (isClient)
                    _clientOnHit?.Invoke();
            }
            _serverTimer = _baseInterval;
        }
    }

    [Server]
    private bool TryHit()
    {
        bool hitAnyone = false;

        Vector3 center = _tank.Rigidbody.position;
        center += (_tank.transform.up * _circleOffset.y) + (_tank.transform.right * _circleOffset.x);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _circleRadius);

        Dictionary<string, string> statusEffects = null;
        List<Rigidbody2D> hitRbList = new();
        foreach (var hit in hits)
        {
            var rb = hit.attachedRigidbody;
            if (!rb) continue;

            if (hit.isTrigger) continue;
            if (rb == _tank.Rigidbody) continue;
            if (hitRbList.Contains(rb)) continue;
            hitRbList.Add(rb);

            if (rb.TryGetComponent(out NetworkTank otherTank))
            {
                if (!_enemyTeams.Contains(otherTank.Parameters.TeamId))
                    continue;

                otherTank.Parameters.ServerDealDamage(
                    _baseDamage.Mul(_tank.Parameters.DamageMultiplier),
                    fromPlayerConnId: connectionToClient?.connectionId
                );

                if (statusEffectResources.Length > 0)
                {
                    statusEffects ??= statusEffectResources.ToDictionary(name => TankStatusEffects.GenerateEffectId(netId.ToString(), name));
                    foreach(var effect in statusEffects)
                    {
                        otherTank.StatusEffects.ServerApplyStatusEffect(effect.Key, effect.Value, connectionToClient?.connectionId, _tank.Parameters.TeamId);
                    }
                }

                hitAnyone = true;
            }
            else if (rb.TryGetComponent(out NetworkStructure structure))
            {
                structure.ServerDealDamage(_baseDamage.Mul(_tank.Parameters.DamageMultiplier));
                hitAnyone = true;
            }
        }
        hitRbList.Clear();
        return hitAnyone;
    }

    [ClientRpc(includeOwner = true)]
    private void RpcOnHit()
    {
        if (gameObject.activeSelf && !_tank.Death.IsDead)
            _clientOnHit?.Invoke();
    }
}