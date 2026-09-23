using UnityEngine;
public class TankShootingRulesDisplay : MonoBehaviour
{
    [SerializeField]
    private NetworkTank tank;
    [SerializeField]
    private GameObject[] ableToShootIndicators;
    [SerializeField]
    private GameObject[] notAbleToShootIndicators;

    private bool _wasAbleToShoot;

    private void OnEnable()
    {
        _wasAbleToShoot = tank.Shoot.ShootingRules && tank.Shoot.ShootingRules.IsAbleToShoot;
        UpdateIndicators(_wasAbleToShoot);
    }

    private void Update()
    {
        bool isAbleToShoot = tank.Shoot.ShootingRules && tank.Shoot.ShootingRules.IsAbleToShoot;
        if (_wasAbleToShoot != isAbleToShoot)
        {
            _wasAbleToShoot = isAbleToShoot;
            UpdateIndicators(isAbleToShoot);
        }
    }

    private void UpdateIndicators(bool isAbleToShoot)
    {
        foreach (var obj in ableToShootIndicators)
            if (obj)
                obj.SetActive(isAbleToShoot);
        foreach (var obj in notAbleToShootIndicators)
            if (obj)
                obj.SetActive(!isAbleToShoot);
    }
}