using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
[RequireComponent(typeof(Rigidbody2D))]
public class Item : NetworkBehaviour
{

    [Header("Parameters")]
    [SerializeField]
    private int lifeTime = 10;

    [Header("Pickup Effect (Child object to retrieve)")]
    [SerializeField]
    private GameObject pickupEffectObject;

    [Header("Animations")]
    [SerializeField]
    private Animation itemAnimation;
    [SerializeField]
    private AnimationClip idleClip;
    [SerializeField]
    private float maxIdleStartDelay;
    [SerializeField]
    private AnimationClip warningClip;
    [SerializeField]
    private float warningDuration;

    [Header("Pickup Display")]
    [SerializeField]
    private SpriteRenderer[] pickUpSprites = new SpriteRenderer[0];
    [SerializeField]
    public float pickUpAllowedAlpha = 1f;
    [SerializeField]
    public float pickUpBlockedAlpha = 0.5f;
    [SerializeField]
    private UnityEvent displayPickupAllowed;
    [SerializeField]
    private UnityEvent displayPickupBlocked;

    public readonly SyncList<string> excludeTeams = new();

    [SyncVar]
    private double _initTime;
    [SyncVar]
    private Vector2 _startPosition;
    [SyncVar]
    private Impulse _impulse;

    private float TimePassed => _initTime > 0 ? (float)(NetworkTime.time - _initTime) : 0;

    private bool _serverIsPickedUp;

    private Rigidbody2D _rigidbody;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    public override void OnStartServer()
    {
        _initTime = NetworkTime.time;
    }

    public override void OnStartClient()
    {
        StartCoroutine(StartIdleAnimation());
    }

    private bool? _canPickCached;

    private void Update()
    {
        if (!isServer && !isClient)
            return;

        var timePassed = TimePassed;

        if (_startPosition == Vector2.zero && (Vector2)transform.position != Vector2.zero)
            _startPosition = transform.position;

        if (_impulse.vector.magnitude != 0 && _impulse.duration > 0)
            transform.position = Vector2.Lerp(_startPosition, _startPosition + _impulse.vector, Mathf.Clamp01(timePassed / (_impulse.duration / 2)));

        if (isServer && timePassed >= lifeTime)
            this.DestroyNetObject();
        if (isClient && timePassed < lifeTime)
        {
            if (timePassed > lifeTime - warningDuration)
            {
                if (itemAnimation.clip != warningClip)
                {
                    itemAnimation.clip = warningClip;
                    itemAnimation.Play();
                }
            }
            else if (maxIdleStartDelay > 0 && TimePassed > maxIdleStartDelay)
            {
                itemAnimation.clip = idleClip;
                itemAnimation.Play();
                maxIdleStartDelay = 0;
            }

            bool canPick = true;

            var localPlayer = NetworkTank.LocalPlayer;
            if (localPlayer && excludeTeams.Contains(localPlayer.Parameters.TeamId))
                canPick = false;

            if (_canPickCached != canPick)
            {
                _canPickCached = canPick;
                foreach (var sprite in pickUpSprites)
                    if (sprite)
                    {
                        var color = sprite.color;
                        color.a = canPick ? pickUpAllowedAlpha : pickUpBlockedAlpha;
                        sprite.color = color;
                    }
                if (canPick)
                    displayPickupAllowed?.Invoke();
                else
                    displayPickupBlocked?.Invoke();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer || !_rigidbody || !other)
            return;

        if (!other.attachedRigidbody)
            return;

        if (!other.attachedRigidbody.TryGetComponent(out NetworkTank tank))
            return;

        if (!_rigidbody.IsTouching(other))
            return;

        if (!ServerCheckPickUpAllowed(tank))
            return;

        ServerOnItemPickUp(tank);
        this.DestroyNetObject();
    }

    private void OnDisable()
    {
        if (gameObject.scene.isLoaded && !AppPlatform.IsHeadless && NetworkClient.active && NetworkTank.LocalPlayer && TimePassed < lifeTime && pickupEffectObject)
        {
            pickupEffectObject.transform.parent = null;
            pickupEffectObject.SetActive(true);
        }
    }

    private IEnumerator StartIdleAnimation()
    {
        if (maxIdleStartDelay > 0)
            yield return new WaitForSeconds(Random.Range(0, maxIdleStartDelay));
        else
            yield return null;
        if (NetworkTime.time - _initTime < lifeTime - warningDuration)
        {
            itemAnimation.clip = idleClip;
            itemAnimation.Play();
        }
    }

    [Server]
    protected virtual bool ServerCheckPickUpAllowed(NetworkTank tank)
    {
        return !_serverIsPickedUp && !excludeTeams.Contains(tank.Parameters.TeamId);
    }

    [Server]
    protected virtual void ServerOnItemPickUp(NetworkTank tank)
    {
        _serverIsPickedUp = true;
    }

    [Server]
    public void ServerSetImpulse(Vector2 impulse, float duration)
    {
        _impulse = new() { vector = impulse, duration = duration };
    }

    [Serializable]
    private struct Impulse
    {
        public Vector2 vector;
        public float duration;
    }

}
