using Mirror;
using UnityEngine;

public class GearItem : Item
{

    [Header("Gear Item")]
    [SerializeField]
    private ushort gearAmount = 1;
    [SerializeField]
    private ushort healAmount = 2;

    [Server]
    protected override void ServerOnItemPickUp(NetworkTank tank)
    {
        base.ServerOnItemPickUp(tank);
        tank.Parameters.ServerAddGears(gearAmount);
        tank.Parameters.ServerApplyHealing(healAmount);
        if (tank.Shoot.ShootingRules && tank.Shoot.ShootingRules is ZapTankShootingRules zapRules)
            zapRules.ServerRestoreStack();
    }

    protected override bool ServerCheckPickUpAllowed(NetworkTank tank)
    {
        return base.ServerCheckPickUpAllowed(tank) && tank.Parameters.CurrentHp > 0;
    }
}
