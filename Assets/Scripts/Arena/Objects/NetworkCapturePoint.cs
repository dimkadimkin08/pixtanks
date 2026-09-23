using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using TMPro;
public class NetworkCapturePoint : NetworkBehaviour
{

    [SerializeField]
    private string[] teamsAllowed;
    [SerializeField]
    private float lockTime = 6;
    [SerializeField]
    private float captureTime = 200;
    [SerializeField]
    private float resetDelay = 10;

    [Space]

    [SerializeField]
    private SpriteRenderer zoneLockSprite;
    [SerializeField]
    private TextMeshPro zoneCaptureText;

    [SyncVar, HideInInspector]
    public string lockTeam;
    [SyncVar, HideInInspector]
    public float lockTimeLeft;
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
        var team = _tanksInZone.Count > 0 ? _tanksInZone[0].Parameters.TeamId : "";
        if (_tanksInZone.All(tank => tank.Parameters.TeamId == team))
        {
            if (lockTeam != team)
            {
                lockTimeLeft = lockTime;
                lockTeam = team;
            }
            else if (lockTimeLeft > 0)
            {
                lockTimeLeft -= Time.fixedDeltaTime;
            }
            else
            {
                if (!captureProgress.ContainsKey(team))
                    captureProgress[team] = 0;
                captureProgress[team] += Time.fixedDeltaTime;
                if (captureProgress[team] >= captureTime)
                    _serverTimeUntilReset = resetDelay;
            }
        }
        else
        {
            lockTeam = "";
        }
    }

    public void Reset()
    {
        captureProgress.Clear();
    }

}