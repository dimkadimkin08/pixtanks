using UnityEngine;
public class TankReloadDisplay : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Reload Display")]
    [SerializeField]
    private Transform reloadBarTransform;
    [SerializeField]
    private Vector3 reloadBarFullPos = Vector3.zero;
    [SerializeField]
    private Vector3 reloadBarEmptyPos = Vector3.zero;
    [SerializeField]
    private Vector3 reloadBarFullScale = Vector3.one;
    [SerializeField]
    private Vector3 reloadBarEmptyScale = Vector3.zero;

    private void Update()
    {
        var progress = tank.Shoot.ReloadProgress;
        reloadBarTransform.localPosition = Vector3.Lerp(reloadBarEmptyPos, reloadBarFullPos, progress);
        reloadBarTransform.localScale = Vector3.Lerp(reloadBarEmptyScale, reloadBarFullScale, progress);
    }
}
