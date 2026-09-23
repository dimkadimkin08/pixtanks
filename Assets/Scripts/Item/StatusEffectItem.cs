using Mirror;
using UnityEngine;

public class StatusEffectItem : Item
{

    [Header("Status Effect Item")]
    [SerializeField]
    private string statusEffectId;
    [SerializeField]
    private string statusEffectName;
    [SerializeField]
    private bool reapplyAllowed = true;

    [Server]
    protected override void ServerOnItemPickUp(NetworkTank tank)
    {
        base.ServerOnItemPickUp(tank);
        tank.StatusEffects.ServerApplyStatusEffect(id: statusEffectId, statusEffectName: statusEffectName, fromConnId: null);
    }

    protected override bool ServerCheckPickUpAllowed(NetworkTank tank)
    {
        return base.ServerCheckPickUpAllowed(tank) && tank.Parameters.CurrentHp > 0 && (reapplyAllowed || !tank.StatusEffects.ContainsEffect(statusEffectId));
    }
}
