using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class TankScanner
{
    private readonly TankAIScannerSettings _settings;
    private readonly Rigidbody2D _tankRigidbody;

    private readonly ContactFilter2D _scanFilter;
    private readonly List<Collider2D> _scanBuffer = new();

    private float _timeSinceLastScan;
    private string[] _enemies;

    public Transform TargetEnemy { get; private set; }

    public TankScanner(string teamId, Rigidbody2D tankRigidbody, TankAIScannerSettings settings)
    {
        _settings = settings;
        _tankRigidbody = tankRigidbody;
        SetTeam(teamId);
        _scanFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = _settings.scanLayerMask
        };
    }

    private void ExecuteScan(TankAIScannerSettings settings)
    {
        TargetEnemy = null;

        int count = Physics2D.OverlapCircle(
            _tankRigidbody.position,
            settings.scanRadius,
            _scanFilter,
            _scanBuffer
        );

        var candidates = new List<(Rigidbody2D body, float distance)>(count);
        for (int i = 0; i < count; i++)
        {
            var rb = _scanBuffer[i].attachedRigidbody;
            if (!rb || rb == _tankRigidbody)
                continue;

            if (!rb.TryGetComponent(out TankParameters parameters) || !_enemies.Contains(parameters.TeamId))
                continue;

            float distance = Vector2.Distance(_tankRigidbody.position, rb.position);
            candidates.Add((rb, distance));
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        foreach (var (targetRigidbody, distanceToTarget) in candidates)
        {
            if (_settings.checkForObstacles)
            {
                var direction = targetRigidbody.position - _tankRigidbody.position;
                var hit = Physics2D.Raycast(
                    _tankRigidbody.position,
                    direction,
                    direction.magnitude,
                    settings.obstaclesLayerMask
                );

                if (hit && hit.rigidbody != _tankRigidbody && hit.rigidbody != targetRigidbody)
                    continue;
            }

            // Нашли подходящую цель — выходим
            TargetEnemy = targetRigidbody.transform;
            break;
        }
    }


    public void SetTeam(string teamId)
    {
        _enemies = GameTanksManager.Singleton.GetTeam(teamId)?.enemies ?? Array.Empty<string>();
    }

    public void Update(float deltaTime)
    {
        _timeSinceLastScan += deltaTime;
        if (_timeSinceLastScan < _settings.scanInterval)
            return;
        _timeSinceLastScan = 0;
        ExecuteScan(_settings);
    }

}
