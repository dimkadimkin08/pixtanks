using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class NetworkCaptureFlag : NetworkBehaviour
{

    public const short STATE_CAPTURED = 0;
    public const short STATE_SCORED = 1;
    public const short STATE_TELEPORTED = 2;
    public const short STATE_DELIVERED = 3;
    public const short STATE_LOST = 4;


    [SerializeField]
    private float deliveryPrivotRadius = 1;
    [SerializeField]
    private float teleportDelay = 5;

    [Space]

    [SerializeField]
    private Vector2 capturedLocalOffset;
    [SerializeField]
    private Vector2 capturedGlobalOffset;

    [Space]

    [SerializeField]
    private TextMeshPro scoreText;
    [SerializeField]
    private GameObject flagDisplayNormal;
    [SerializeField]
    private GameObject flagDisplayCaptured;
    [SerializeField]
    private SpriteRenderer[] teamColoredSprites;
    [SerializeField]
    private ParticleSystem[] teamColoredParticles;
    [SerializeField]
    private Image[] teamColoredUIImages;

    [Space]

    public UnityEvent<int> serverOnScore;

    [Space]

    [SerializeField]
    private UnityEvent[] clientOnCapture;
    [SerializeField]
    private UnityEvent[] clientOnDelivery;
    [SerializeField]
    private UnityEvent[] clientOnLost;
    [SerializeField]
    private UnityEvent[] clientOnScore;
    [SerializeField]
    private UnityEvent[] clientOnTeleport;

    [Space]

    [SerializeField]
    private Loot[] lootList;

    [HideInInspector]
    private string[] serverEnemyTeams;
    [HideInInspector]
    public Vector2 serverStartPrivot;
    [HideInInspector]
    public List<NetworkCaptureFlag> serverOtherFlags;


    [SyncVar(hook=nameof(HookTeam)), HideInInspector]
    private string team;
    [SyncVar, HideInInspector]
    public int lastState = STATE_TELEPORTED;
    [SyncVar, HideInInspector]
    public bool isCaptured;
    [SyncVar, HideInInspector]
    public int score;

    private NetworkTank _serverDroppedByTank;
    private double _serverDroppedAt;
    private NetworkTank _serverTargetTank;
    private float _serverTimeUntilTeleport;
    private bool _clientWasCaptured;

    public override void OnStartServer()
    {
        transform.position = serverStartPrivot;
    }

    private void OnEnable()
    {
        _clientWasCaptured = isCaptured;
        flagDisplayNormal.SetActive(!isCaptured);
        flagDisplayCaptured.SetActive(isCaptured);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer)
            return;
        if (isCaptured && _serverTargetTank && !_serverTargetTank.Death.IsDead)
            return;
        var rigidbody = collision.attachedRigidbody;
        if (!rigidbody)
            return;
        if (!rigidbody.TryGetComponent(out NetworkTank tank))
            return;
        if (tank.Parameters.TeamId == team && isCaptured)
        {
            transform.position = serverStartPrivot;
            isCaptured = false;
            NotifyStateChanged(STATE_TELEPORTED);
            return;
        }
        if (!serverEnemyTeams.Contains(tank.Parameters.TeamId))
            return;
        if (_serverDroppedByTank == tank && 1f > NetworkTime.time - _serverDroppedAt)
            return;
        _serverTargetTank = tank;
        isCaptured = true;
        NotifyStateChanged(STATE_CAPTURED);
    }

    private void Update()
    {
        if (!AppPlatform.IsHeadless)
        {
            if (scoreText)
                scoreText.text = !isCaptured ? score.ToString() : "";
            if (_clientWasCaptured != isCaptured)
            {
                _clientWasCaptured = isCaptured;
                flagDisplayNormal.SetActive(!isCaptured);
                flagDisplayCaptured.SetActive(isCaptured);
            }
        }
        if (!isServer)
            return;
        if (TryDeliverFlag())
            return;
        if (_serverTargetTank && !_serverTargetTank.Death.IsDead && _serverTargetTank.Special.ServerGetSpecialPressed())
        {
            _serverDroppedByTank = _serverTargetTank;
            _serverDroppedAt = NetworkTime.time;
            _serverTargetTank = null;
        }
        if (!_serverTargetTank || _serverTargetTank.Death.IsDead)
        {
            if (isCaptured && _serverTimeUntilTeleport <= 0)
            {
                _serverTimeUntilTeleport = teleportDelay;
                NotifyStateChanged(STATE_LOST);
            }
            else
            {
                _serverTimeUntilTeleport -= Time.deltaTime;
                if (_serverTimeUntilTeleport <= 0 && 0.01f < Vector2.Distance(serverStartPrivot, transform.position))
                {
                    transform.position = serverStartPrivot;
                    isCaptured = false;
                    NotifyStateChanged(STATE_TELEPORTED);
                }
            }
        }
        else
        {
            var offset = Vector3.zero;
            offset += _serverTargetTank.transform.up * capturedLocalOffset.y;
            offset += _serverTargetTank.transform.right * capturedLocalOffset.x;
            transform.position = _serverTargetTank.transform.position + offset + (Vector3)capturedGlobalOffset;
            _serverTimeUntilTeleport = 0;
        }
    }

    private bool TryDeliverFlag()
    {
        if (!isCaptured)
            return false;

        float radiusSqr = deliveryPrivotRadius * deliveryPrivotRadius;

        Vector2 myPos = transform.position;
        Vector2 tankPos = _serverTargetTank ? (Vector2)_serverTargetTank.transform.position : default;

        foreach (var otherFlag in serverOtherFlags)
        {
            if (otherFlag == null || otherFlag.isCaptured)
                continue;

            Vector2 deliveryPoint = otherFlag.serverStartPrivot;

            if ((myPos - deliveryPoint).sqrMagnitude < radiusSqr)
            {
                Deliver(otherFlag);
                return true;
            }

            if (_serverTargetTank && (tankPos - deliveryPoint).sqrMagnitude < radiusSqr)
            {
                Deliver(otherFlag);
                return true;
            }
        }

        return false;
    }

    private void Deliver(NetworkCaptureFlag otherFlag)
    {
        transform.position = serverStartPrivot;
        _serverTargetTank = null;
        isCaptured = false;

        NotifyStateChanged(STATE_DELIVERED);
        otherFlag.ServerScoreUp();
    }

    [Server]
    public void ServerReset()
    {
        _serverTargetTank = null;
        transform.position = serverStartPrivot;
        isCaptured = false;
        score = 0;
        serverOnScore?.Invoke(score);
    }

    [Server]
    public void ServerScoreUp()
    {
        score++;
        int lootSeed = Mathf.RoundToInt(UnityEngine.Random.value * 100);
        foreach (var loot in lootList)
        {
            for (int i = 0; i < loot.count; i++)
            {
                var impulse = GameItemsManager.GetSpreadImpulse(
                    i,
                    loot.count,
                    loot.impulseDuration,
                    seed: lootSeed
                );

                GameItemsManager.Singleton.ServerSpawnItem(
                    loot.itemId,
                    transform.position,
                    impulse,
                    loot.impulseStrength
                );
            }
        }
        serverOnScore?.Invoke(score);
        NotifyStateChanged(STATE_SCORED);
    }

    private void NotifyStateChanged(short state)
    {
        lastState = state;
        ClientRpcOnStateChanged(state);
        if (isHost)
            ClientOnStateChanged(state);
    }

    [ClientRpc]
    private void ClientRpcOnStateChanged(short state)
    {
        ClientOnStateChanged(state);
    }

    [Client]
    private void ClientOnStateChanged(short state)
    {
        switch (state)
        {
            case STATE_CAPTURED:
                foreach (var e in clientOnCapture)
                    e?.Invoke();
                break;

            case STATE_DELIVERED:
                foreach (var e in clientOnDelivery)
                    e?.Invoke();
                break;

            case STATE_SCORED:
                foreach (var e in clientOnScore)
                    e?.Invoke();
                break;

            case STATE_TELEPORTED:
                foreach (var e in clientOnTeleport)
                    e?.Invoke();
                break;

            case STATE_LOST:
                foreach (var e in clientOnLost)
                    e?.Invoke();
                break;
        }
    }

    private void HookTeam(string old, string current)
    {
        ShowTeamColor(current);
    }

    private void ShowTeamColor(string teamId)
    {
        var teamObj = GameTanksManager.Singleton.GetTeam(teamId);
        scoreText.color = new(teamObj.color.r, teamObj.color.g, teamObj.color.b, scoreText.color.a);
        foreach (var sprite in teamColoredSprites)
            sprite.color = new(teamObj.color.r, teamObj.color.g, teamObj.color.b, sprite.color.a);
        foreach (var image in teamColoredUIImages)
            image.color = new(teamObj.color.r, teamObj.color.g, teamObj.color.b, image.color.a);
        foreach (var effect in teamColoredParticles)
            effect.SetStartColorKeepAlpha(teamObj.color);
    }

    public void SetTeam(string teamId)
    {
        var teamObj = GameTanksManager.Singleton.GetTeam(teamId);
        team = teamObj.id;
        serverEnemyTeams = teamObj.enemies;
        if (isClient)
            ShowTeamColor(teamId);
    }

    [Serializable]
    private class Loot
    {
        public float impulseStrength;
        public float impulseDuration;
        public int count;
        public string itemId;
    }

}