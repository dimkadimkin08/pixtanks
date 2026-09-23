using UnityEngine;
using Mirror;
public class StructureSelfDestruct : NetworkBehaviour
{
    [SerializeField]
    private NetworkStructure structure;
    [SerializeField]
    private ushort damagePerTick = 1;
    [SerializeField]
    private float tickInterval = 1;
    [SerializeField]
    private bool dropLootOnIgnore = false;

    private double _serverTickTimer = -1;
    private double _serverLastHp;
    private bool _serverIsIgnored = true;

    public override void OnStartServer()
    {
        _serverTickTimer = tickInterval;
        _serverLastHp = structure.CurrentHp;
    }

    private void Update()
    {
        if (!isServer || _serverTickTimer < 0)
            return;
        _serverTickTimer -= Time.deltaTime;
        if (_serverTickTimer <= 0)
        {
            _serverTickTimer = tickInterval;
            _serverIsIgnored = _serverIsIgnored && _serverLastHp == structure.CurrentHp;
            if (structure.CurrentHp > damagePerTick)
                structure.ServerDealDamage(damagePerTick);
            else
                structure.ServerKillStructure(preventLootDrop: !dropLootOnIgnore && _serverIsIgnored);
            _serverLastHp = structure.CurrentHp;
        }
    }
}