using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static NetworkEscortPayload;
public class NetworkEscortPayload : NetworkBehaviour
{
    [SerializeField]
    private RoutePoint[] route;

    [Space]

    [SerializeField]
    private string team;
    [SerializeField]
    private string enemyTeam;

    [Space]

    [SerializeField]
    private float baseSpeed = 2;
    [SerializeField]
    private float playerSpeedBoost = 0.5f;
    [SerializeField]
    private float maxBoostPlayers = 4;
    [SerializeField]
    private float moveSpeedAcceleration = 2;

    [Space]

    [SerializeField]
    private float backSpeed = 1;

    [Space]

    [SerializeField]
    private Transform rotationDisplayBody;
    [SerializeField]
    private float rotationSpeed = 30;
    [SerializeField]
    private SpriteRenderer[] teamColoredSprites;
    [SerializeField]
    private ParticleSystem[] teamColoredEffects;
    [SerializeField]
    private Color defaultColor = Color.white;
    [SerializeField]
    private TextMeshPro allyCountText;
    [SerializeField]
    private GameObject moveIndicator;
    [SerializeField]
    private GameObject backMoveIndicator;

    [Space]


    [SerializeField]
    private CheckPointEvent[] ServerCheckPointEvents;

    [Space]

    [SerializeField]
    private UnityEvent ClientOnStartMoving;
    [SerializeField]
    private UnityEvent ClientOnStopMoving;
    [SerializeField]
    private UnityEvent<int> ClientOnCheckPoint;
    [SerializeField]
    private UnityEvent ClientOnFinish;

    [SyncVar, HideInInspector]
    public int allyTanksCount;
    [SyncVar]
    private int bodyRotationZ;
    [SyncVar]
    private int nextPoint;
    [SyncVar]
    private bool isFinished;

    private Vector2 _clientLastPos;
    private float _clientTimeUntilPosCheck;
    private bool _clientIsMoving;
    private int _clientLastAllyCount;

    private readonly List<NetworkTank> _serverNearbyTanks = new();
    private float _serverMoveSpeed;

    private RouteProgressCalculator _progressCalc;

    public float Progress => isFinished ? 1 : _progressCalc.GetProgress(transform.position, nextPoint);

    public override void OnStartClient()
    {
        ClientUpdateDisplay(force: true);
    }

    private void OnEnable()
    {
        _progressCalc = new RouteProgressCalculator(route);
        if (isClient)
            ClientUpdateDisplay(force: true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer || !collision.attachedRigidbody)
            return;
        if (!collision.attachedRigidbody.TryGetComponent(out NetworkTank tank) || _serverNearbyTanks.Contains(tank))
            return;
        _serverNearbyTanks.Add(tank);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!isServer || !collision.attachedRigidbody)
            return;
        if (!collision.attachedRigidbody.TryGetComponent(out NetworkTank tank))
            return;
        _serverNearbyTanks.Remove(tank);
    }

    private void Update()
    {
        if (isClient)
        {
            ClientUpdateDisplay(Time.deltaTime);
        }
        if (isServer)
        {
            ServerRefreshAllyTanksCount();
            ServerMovePayload(Time.deltaTime);
        }
    }

    [Client]
    private void ClientUpdateDisplay(float delta = 0, bool force = false)
    {
        _clientTimeUntilPosCheck -= delta;
        var movingChanged = false;
        if (_clientTimeUntilPosCheck < 0)
        {
            _clientTimeUntilPosCheck = 0.1f;
            var isMoving = 0.001f < Vector2.Distance(transform.position, _clientLastPos);
            movingChanged = isMoving != _clientIsMoving;
            _clientIsMoving = isMoving;
            _clientLastPos = transform.position;
        }
        if (force || movingChanged)
        {
            if (_clientIsMoving)
                ClientOnStartMoving?.Invoke();
            else
                ClientOnStopMoving?.Invoke();
        }
        if (force || _clientLastAllyCount != allyTanksCount || movingChanged)
        {
            if (allyCountText)
                allyCountText.text = allyTanksCount > 0 ? allyTanksCount.ToString() : "";
            if (moveIndicator)
                moveIndicator.SetActive(allyTanksCount > 0 && _clientIsMoving);
            if (backMoveIndicator)
                backMoveIndicator.SetActive(allyTanksCount < 0 && _clientIsMoving);
            var color = allyTanksCount == 0
                ? defaultColor
                : GameTanksManager.Singleton.GetTeam(allyTanksCount > 0 ? team : enemyTeam).color;
            foreach (var spriteRenderer in teamColoredSprites)
                if (spriteRenderer)
                    spriteRenderer.color = new Color(color.r, color.g, color.b, spriteRenderer.color.a);
            foreach (var effect in teamColoredEffects)
                if (effect)
                    effect.SetStartColorKeepAlpha(color);
        }
        if (rotationDisplayBody)
        {
            if (delta == 0)
                rotationDisplayBody.eulerAngles = new(0, 0, bodyRotationZ);
            else
                rotationDisplayBody.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.MoveTowardsAngle(rotationDisplayBody.eulerAngles.z, bodyRotationZ, rotationSpeed * 10 * Time.deltaTime));
        }
    }

    [ClientRpc]
    private void ClientRpcOnCheckPoint(int number)
    {
        if (number >= 0)
            ClientOnCheckPoint?.Invoke(number);
        else
            ClientOnFinish?.Invoke();
    }

    [Server]
    private void ServerRefreshAllyTanksCount()
    {
        _serverNearbyTanks.RemoveAll(tank => !tank || tank.Death.IsDead);
        int allyTanks = 0;
        int enemyTanks = 0;
        foreach (var tank in _serverNearbyTanks)
        {
            if (!tank || tank.Death.IsDead)
                continue;
            if (team == tank.Parameters.TeamId)
                allyTanks++;
            else if (enemyTeam == tank.Parameters.TeamId)
                enemyTanks++;
        }
        allyTanksCount = Math.Sign(allyTanks) != Math.Sign(enemyTanks)
            ? Mathf.RoundToInt(Mathf.Clamp(allyTanks - enemyTanks, -1, maxBoostPlayers))
            : 0;
    }

    [Server]
    private void ServerMovePayload(float delta)
    {
        if (route.Length <= 1 || allyTanksCount == 0 || isFinished)
        {
            _serverMoveSpeed = 0;
            return;
        }
        if (nextPoint >= route.Length || nextPoint <= 0)
            nextPoint = 1;

        var currentPos = transform.position;
        var moveForward = allyTanksCount > 0;

        if (moveForward)
        {
            if (0.1f > Vector2.Distance(currentPos, route[nextPoint].privot.position))
            {
                if (nextPoint < route.Length - 1)
                {
                    if (route[nextPoint].isCheckPoint)
                    {
                        foreach (var checkPointEvent in ServerCheckPointEvents)
                            if (checkPointEvent.routePointIndex == nextPoint)
                                checkPointEvent.action?.Invoke();
                        ClientRpcOnCheckPoint(nextPoint);
                    }
                    nextPoint++;
                }
                else if (nextPoint == route.Length - 1 && !isFinished)
                {
                    isFinished = true;
                    ClientRpcOnCheckPoint(-1); // Finish
                }
            }
        }


        if (!moveForward)
            if (!route[nextPoint - 1].isCheckPoint)
                if (0.1f > Vector2.Distance(currentPos, route[nextPoint - 1].privot.position))
                    if (nextPoint > 1)
                    {
                        isFinished = false;
                        nextPoint--;
                    }

        var targetPoint = route[moveForward ? nextPoint : nextPoint - 1];

        if (isFinished || 0.1f > Vector2.Distance(currentPos, targetPoint.privot.position))
        {
            _serverMoveSpeed = 0;
            return;
        }

        var targetSpeed = moveForward ? baseSpeed + ((allyTanksCount - 1) * playerSpeedBoost) : backSpeed;
        _serverMoveSpeed = Mathf.MoveTowards(_serverMoveSpeed, targetSpeed, moveSpeedAcceleration * delta);

        transform.position = Vector2.MoveTowards(currentPos, targetPoint.privot.position, _serverMoveSpeed * delta);

        Vector2 dir = targetPoint.privot.position - currentPos;
        int rot = Mathf.RoundToInt(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        if (!moveForward)
        {
            rot += 180;
        }
        bodyRotationZ = rot;
    }

    [Server]
    public void ServerReset()
    {
        isFinished = false;
        allyTanksCount = 0;
        nextPoint = 1;
        if (route.Length > 0)
            transform.position = route[0].privot.position;
    }

    [Serializable]
    public class CheckPointEvent
    {
        public int routePointIndex;
        public UnityEvent action;
    }

    [Serializable]
    public class RoutePoint
    {
        public Transform privot;
        public bool isCheckPoint;
    }


    public class RouteProgressCalculator
    {
        private RoutePoint[] _points;
        private float[] _segmentLengths;
        private float[] _cumulativeLengths;
        private float _totalLength;

        public RouteProgressCalculator(RoutePoint[] points)
        {
            _points = points;
            Precompute();
        }

        private void Precompute()
        {
            int n = _points.Length;
            _segmentLengths = new float[n - 1];
            _cumulativeLengths = new float[n];

            _cumulativeLengths[0] = 0f;

            for (int i = 0; i < n - 1; i++)
            {
                float d = Vector3.Distance(_points[i].privot.position,
                                           _points[i + 1].privot.position);

                _segmentLengths[i] = d;
                _cumulativeLengths[i + 1] = _cumulativeLengths[i] + d;
            }

            _totalLength = _cumulativeLengths[n - 1];
        }

        public float GetProgress(Vector3 cartPos, int nextPointIndex)
        {
            if (nextPointIndex <= 0)
                return 0f;

            if (nextPointIndex >= _points.Length)
                return 1f;

            int prev = nextPointIndex - 1;

            Vector3 a = _points[prev].privot.position;
            Vector3 b = _points[nextPointIndex].privot.position;

            float segLen = _segmentLengths[prev];
            float t = 0f;

            if (segLen > 0.0001f)
            {
                Vector3 ab = b - a;
                Vector3 ac = cartPos - a;
                t = Mathf.Clamp01(Vector3.Dot(ac, ab) / (segLen * segLen));
            }

            float passed = _cumulativeLengths[prev] + segLen * t;

            return passed / _totalLength;
        }
    }
}