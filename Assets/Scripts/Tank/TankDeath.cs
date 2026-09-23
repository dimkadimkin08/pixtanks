using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;
public class TankDeath : NetworkBehaviour
{

    [Header("Tank")]

    [SerializeField]
    private NetworkTank tank;

    [Header("Death state")]

    [SerializeField]
    private float deathStateDuration = 2;
    [SerializeField]
    private bool disableRigidbodyOnDeath;
    [SerializeField]
    private MonoBehaviour[] disableDuringDeathState;
    [SerializeField]
    private GameObject[] inactiveDuringDeathState;

    [Header("Loot")]
    [SerializeField]
    private Loot[] lootList;

    [SyncVar(hook = nameof(HookDeathState))]
    private bool _deathState;

    public bool IsDead => _deathState;

    public event Action OnDeath;

    private double _serverDestroyAt = -1;

    public override void OnStartServer()
    {
        if (tank.Parameters)
            tank.Parameters.OnCurrentHpSet += ServerOnCurrentHpSet;
    }

    public override void OnStopServer()
    {
        if (tank.Parameters)
            tank.Parameters.OnCurrentHpSet -= ServerOnCurrentHpSet;
    }

    private void OnDestroy()
    {
        OnDeath = null;
    }

    [Server]
    private void ServerOnCurrentHpSet(ushort oldHp, ushort currentHp)
    {
        if (currentHp == 0)
            ServerKillTank();
    }

    [Server]
    private void ServerSpawnLoot(bool preventAllyPickup = false)
    {
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
                    loot.impulseStrength,
                    excludeTeams: preventAllyPickup && !GameTanksManager.Singleton.GetTeam(tank.Parameters.TeamId).enemies.Contains(tank.Parameters.TeamId)
                        ? new string[] { tank.Parameters.TeamId }
                        : null
                );
            }
        }
    }

    private void Update()
    {
        if (isServer && _serverDestroyAt > 0 && _serverDestroyAt < NetworkTime.time)
        {
            _serverDestroyAt = -1;
            if (connectionToClient == null || connectionToClient.identity != netIdentity)
                this.DestroyNetObject();
            else
                NetworkServer.RemovePlayerForConnection(connectionToClient, RemovePlayerOptions.Destroy);
        }
    }

    private void HookDeathState(bool oldValue, bool deathState)
    {
        if (isClientOnly && deathState && !oldValue)
            ApplyDeathState();
    }

    private void ApplyDeathState()
    {
        OnDeath?.Invoke();
        OnDeath = null;
        if (disableRigidbodyOnDeath)
        {
            tank.Rigidbody.bodyType = RigidbodyType2D.Kinematic;
            tank.Rigidbody.simulated = false;
        }
        foreach (var script in disableDuringDeathState)
            if (script)
                script.enabled = false;
        foreach (var obj in inactiveDuringDeathState)
            if (obj)
                obj.SetActive(false);
    }

    [Server]
    public void ServerKillTank(bool preventLootDrop = false, bool preventAllyLootPickup = false)
    {
        if (_deathState)
            return;
        _deathState = true;
        ApplyDeathState();
        if (!preventLootDrop)
            ServerSpawnLoot(preventAllyPickup: preventAllyLootPickup);
        _serverDestroyAt = NetworkTime.time + deathStateDuration;
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
