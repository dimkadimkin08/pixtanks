using UnityEngine;
using Mirror;
using UnityEngine.Events;
public class AttackDeflecter : NetworkBehaviour
{
    [SerializeField] private Attack baseAttack;
    [SerializeField] private UnityEvent clientOnDeflect;

    [Server]
    public void ServerTryDeflectAttack(Attack otherAttack)
    {
        if (otherAttack is ProjectileAttack oldProjectile)
        {
            var prefab = oldProjectile.ServerSelfPrefab;
            var position = oldProjectile.transform.position;
            var rotation = Quaternion.Euler(oldProjectile.transform.eulerAngles + new Vector3(0, 0, 180));
            oldProjectile.DestroyNetObject();
            var newProjectileObj = Instantiate(prefab, position, rotation);
            var newProjectile = newProjectileObj.GetComponent<ProjectileAttack>();
            newProjectile.ServerInit(
                 prefab,
                 baseAttack.ServerOwnerConnId,
                 baseAttack.ServerOwnerNetId,
                 baseAttack.currentTeamId,
                 baseAttack.ServerDamageMultiplier,
                 baseAttack.ServerOwnerRigidbody);
            NetworkServer.Spawn(newProjectile.gameObject);
            RpcOnDeflect();
            if (isClient)
                clientOnDeflect?.Invoke();
        }
    }

    [ClientRpc(includeOwner = true)]
    private void RpcOnDeflect()
    {
        clientOnDeflect?.Invoke();
    }
}