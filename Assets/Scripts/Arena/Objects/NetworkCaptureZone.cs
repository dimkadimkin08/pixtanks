using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
public class NetworkCaptureZone : NetworkBehaviour
{

    [SerializeField]
    private string[] teamsAllowed;
    [SerializeField]
    private float captureSpeed = 5;
    [SerializeField]
    private float captureLimit = 1000;
    [SerializeField]
    private float resetDelay = 10;

    public readonly SyncDictionary<string, float> captureProgress = new();

    private List<NetworkTank> _tanksInZone = new();

    private float _serverTimeUntilReset;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer)
            return;
        var rigidbody = collision.attachedRigidbody;
        if (!rigidbody)
            return;
        if (_tanksInZone.Any(tank => tank.Rigidbody == rigidbody))
            return;
        if (!rigidbody.TryGetComponent(out NetworkTank tank))
            return;
        _tanksInZone.Add(tank);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!isServer)
            return;
        var rigidbody = collision.attachedRigidbody;
        if (!rigidbody)
            return;
        var index = _tanksInZone.FindIndex(tank => tank.Rigidbody == rigidbody);
        if (index >= 0)
            _tanksInZone.RemoveAt(index);
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;
        if (_serverTimeUntilReset > 0)
        {
            _serverTimeUntilReset -= Time.fixedDeltaTime;
            if (_serverTimeUntilReset <= 0)
                captureProgress.Clear();
            return;
        }
        foreach(var tank in _tanksInZone)
        {
            var team = tank.Parameters.TeamId;
            if (!captureProgress.ContainsKey(team))
                captureProgress[team] = 0;
            captureProgress[team] = Mathf.Clamp(captureProgress[team] + captureSpeed * Time.fixedDeltaTime, 0, captureLimit);
            if (captureProgress[team] == captureLimit)
                _serverTimeUntilReset = resetDelay;
        }
    }

    public void Reset()
    {
        captureProgress.Clear();
    }

}