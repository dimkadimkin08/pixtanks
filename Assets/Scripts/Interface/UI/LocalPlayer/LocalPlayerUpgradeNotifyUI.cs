using UnityEngine;
public class LocalPlayerUpgradeNotifyUI : MonoBehaviour
{
    [SerializeField]
    private GameObject notificationObject;

    private bool _wasAbleToUpgrade;

    private void OnEnable()
    {
        if (notificationObject)
            notificationObject.SetActive(_wasAbleToUpgrade);
    }

    private void Update()
    {
        if (notificationObject)
        {
            var ableToUpgrade = NetworkShop.Singleton && NetworkShop.Singleton.LocalPlayerCanUpgrade;
            if (ableToUpgrade != _wasAbleToUpgrade)
            {
                _wasAbleToUpgrade = ableToUpgrade;
                notificationObject.SetActive(ableToUpgrade);
            }
        }
    }
}