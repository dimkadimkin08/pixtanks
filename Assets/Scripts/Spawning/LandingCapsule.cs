using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class LandingCapsule : NetworkBehaviour
{

    public static System.Action<LandingCapsule> OnLocalPlayerLandingCapsule;

    [Header("Components")]
    [SerializeField]
    private Rigidbody2D capsuleRigidbody;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private SpriteRenderer[] spriteRenderersToSetColor;

    [Header("Time")]
    [SerializeField]
    private float startImpulseDuration = 1.5f;
    [SerializeField]
    private float landDelay = 3;
    [SerializeField]
    private float spawnDelay = 4;
    [SerializeField]
    private float selfDestructDelay = 5;

    [Header("Random Start Impulse")]
    [SerializeField]
    private float minImpulseForce = 100;
    [SerializeField]
    private float maxImpulseForce = 200;

    [Header("Landing")]
    [SerializeField]
    private ushort landDamage = 5;
    [SerializeField]
    private float landAreaRadius = 2;
    [SerializeField]
    private LayerMask landDamageLayerMask;
    [SerializeField]
    private float tankPushForce = 200;
    [SerializeField]
    private float rigidbodyPushForce = 20000;

    [SyncVar(hook = nameof(HookCapsuleCreatedAt))]
    private double capsuleCreatedAt;
    [SyncVar(hook = nameof(HookCapsuleTeam))]
    private string capsuleTeamId = "";

    private bool _isSpawned;

    public override void OnStartServer()
    {
        capsuleCreatedAt = NetworkTime.time;
        if (isClient)
        {
            ClientJumpToAnimationTime();
            UpdateVisualColors();
        }
    }

    public override void OnStartClient()
    {
        ClientJumpToAnimationTime();
        UpdateVisualColors();
        if (isLocalPlayer)
            OnLocalPlayerLandingCapsule?.Invoke(this);
    }

    private void OnEnable()
    {
        if (isClient)
        {
            ClientJumpToAnimationTime();
            UpdateVisualColors();
        }
    }

    private void UpdateVisualColors()
    {
        if (capsuleTeamId == "")
            return;
        var capsuleTeam = GameTanksManager.Singleton.GetTeam(capsuleTeamId);
        if (capsuleTeam == default)
            return;
        var capsuleColor = capsuleTeam.color;
        foreach (var spriteRenderer in spriteRenderersToSetColor)
            if (spriteRenderer)
                spriteRenderer.color = new Color(capsuleColor.r, capsuleColor.g, capsuleColor.b, spriteRenderer.color.a);
    }

    [Client]
    private void HookCapsuleCreatedAt(double old, double current)
    {
        ClientJumpToAnimationTime();
    }

    [Client]
    private void HookCapsuleTeam(string old, string current)
    {
        UpdateVisualColors();
    }

    [Client]
    private void ClientJumpToAnimationTime()
    {
        if (!animator || capsuleCreatedAt == 0)
            return;
        AnimationClip currAnim = null;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(clip.name))
                currAnim = clip;
        var timePassed = (float)System.Math.Clamp(NetworkTime.time - capsuleCreatedAt, 0, float.MaxValue);
        try
        {
            animator.Play(currAnim.name, 0, Mathf.Clamp01(timePassed / currAnim.averageDuration));
        }
        catch
        {

        }
    }

    [Server]
    private IEnumerator ServerLanding(string[] enemyTeams)
    {
        yield return new WaitForSeconds(landDelay);
        bool checkTeam(NetworkTank tank) => enemyTeams.Length == 0 || enemyTeams.Contains(tank.Parameters.TeamId);
        var rigidbodyHits = new List<Rigidbody2D>();
        foreach (var hit in Physics2D.OverlapCircleAll(capsuleRigidbody.position, landAreaRadius, landDamageLayerMask.value))
        {
            if (hit.attachedRigidbody && !rigidbodyHits.Contains(hit.attachedRigidbody))
            {
                rigidbodyHits.Add(hit.attachedRigidbody);
                if (hit.attachedRigidbody.TryGetComponent(out NetworkTank tank) && checkTeam(tank))
                {
                    tank.Parameters.ServerDealDamage(landDamage);
                    var direction = (tank.Rigidbody.position - capsuleRigidbody.position).normalized;
                    tank.Movement.ServerAddForce(direction * tankPushForce);
                }
                if (hit.attachedRigidbody.TryGetComponent(out NetworkStructure structure))
                {
                    structure.ServerDealDamage(landDamage);
                    var direction = (structure.Rigidbody.position - capsuleRigidbody.position).normalized;
                    structure.Rigidbody.AddForce(direction * rigidbodyPushForce);
                }
            }
        }
    }

    [Server]
    private IEnumerator ServerImpulse()
    {
        capsuleRigidbody.AddForce(Random.insideUnitCircle * Random.Range(minImpulseForce, maxImpulseForce));
        yield return new WaitForSeconds(startImpulseDuration);
        capsuleRigidbody.linearVelocity = Vector2.zero;
        capsuleRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    [Server]
    private IEnumerator ServerSpawning(System.Action spawn)
    {
        yield return new WaitForSeconds(spawnDelay);
        spawn();
        _isSpawned = true;
    }

    [Server]
    private IEnumerator ServerSelfDestruct(System.Action onDestroy)
    {
        yield return new WaitForSeconds(selfDestructDelay);
        this.DestroyNetObject();
    }

    [Server]
    public void ServerInit(InitData initData)
    {
        if (!initData.disableImpulse)
            StartCoroutine(ServerImpulse());
        if (initData is BotTankInitData botTankData)
        {
            var tanksManager = GameTanksManager.Singleton;
            var team = tanksManager.GetTeam(botTankData.teamId);
            var enemyTeams = team?.enemies ?? System.Array.Empty<string>();
            if (team != null)
            {
                capsuleTeamId = botTankData.teamId;
                if (isClient)
                    UpdateVisualColors();
            }
            StartCoroutine(ServerLanding(enemyTeams));
            StartCoroutine(ServerSpawning(spawn: () =>
            {
                var tank = tanksManager.ServerSpawnTank(
                    teamId: botTankData.teamId,
                    tankId: botTankData.tankId,
                    position: capsuleRigidbody.position,
                    noAi: botTankData.noAi
                );
                if (tank)
                    botTankData.callback?.Invoke(tank);
                else
                    botTankData.onError?.Invoke();
            }));
            StartCoroutine(ServerSelfDestruct(onDestroy: () =>
            {
                if (!_isSpawned)
                    botTankData.onError?.Invoke();
            }));
        }
        else if (initData is PlayerTankInitData playerTankData)
        {
            var tanksManager = GameTanksManager.Singleton;
            var team = tanksManager.GetTeam(playerTankData.teamId);
            var enemyTeams = team?.enemies ?? System.Array.Empty<string>();
            if (team != null)
            {
                capsuleTeamId = playerTankData.teamId;
                if (isClient)
                    UpdateVisualColors();
            }
            StartCoroutine(ServerLanding(enemyTeams));
            StartCoroutine(ServerSpawning(spawn: () =>
            {
                if (NetworkServer.connections.TryGetValue(playerTankData.connectionId, out NetworkConnectionToClient conn))
                {
                    if (conn.identity == netIdentity)
                        NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.KeepActive);
                    if (tanksManager.ServerSpawnPlayerTank(conn, capsuleRigidbody.position, angle: transform.eulerAngles.z, checkCapsules: false)
                        && conn.identity.TryGetComponent(out NetworkTank tank))
                        playerTankData.callback?.Invoke(tank);
                    else
                        playerTankData.onError?.Invoke();
                }
                else
                    playerTankData.onError?.Invoke();
            }));
            StartCoroutine(ServerSelfDestruct(onDestroy: () =>
            {
                if (!_isSpawned)
                    playerTankData.onError?.Invoke();
            }));
        }
        else if (initData is StructureInitData structureData)
        {
            StartCoroutine(ServerLanding(System.Array.Empty<string>()));
            StartCoroutine(ServerSpawning(spawn: () =>
            {
                var structure = GameStructuresManager.Singleton.ServerSpawnStructure(structureData.structureId, capsuleRigidbody.position);
                if (structure != null && structure)
                    structureData.callback?.Invoke(structure);
                else
                    structureData.onError?.Invoke();
            }));
            StartCoroutine(ServerSelfDestruct(onDestroy: () =>
            {
                if (!_isSpawned)
                    structureData.onError?.Invoke();
            }));
        }
    }

    public abstract class InitData
    {
        public bool disableImpulse;
    }

    public class PlayerTankInitData : InitData
    {
        public int connectionId;
        public string teamId;
        public System.Action<NetworkTank> callback;
        public System.Action onError;
    }

    public class BotTankInitData : InitData
    {
        public bool noAi;
        public string tankId;
        public string teamId;
        public System.Action<NetworkTank> callback;
        public System.Action onError;
    }

    public class StructureInitData : InitData
    {
        public string structureId;
        public System.Action<NetworkStructure> callback;
        public System.Action onError;
    }

}