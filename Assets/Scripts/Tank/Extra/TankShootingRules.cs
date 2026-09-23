using Mirror;
public abstract class TankShootingRules : NetworkBehaviour
{
    public abstract bool IsAbleToShoot { get; }
}