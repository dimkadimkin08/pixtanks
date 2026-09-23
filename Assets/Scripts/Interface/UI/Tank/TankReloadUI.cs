using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
public class TankReloadUI : MonoBehaviour
{
    [Header("Tank")]
    public NetworkTank tank;

    [Header("Reload UI")]
    [SerializeField]
    private Text reloadText;
    [SerializeField]
    private Image reloadBarImage;

    private float lastProgress = 0;

    private void Update()
    {
        if (!tank)
            return;
        var progress = tank.Shoot.ReloadProgress;
        var newProgress = lastProgress >= progress ? progress : Mathf.MoveTowards(lastProgress, progress, Time.deltaTime * 10);
        if (reloadText)
            reloadText.text = newProgress < 0 ? (Mathf.Round(tank.Shoot.ReloadTime * (1f - newProgress) * 10) / 10).ToString(CultureInfo.InvariantCulture) : "";
        if (reloadBarImage)
            reloadBarImage.fillAmount = newProgress;
        lastProgress = progress;
    }
}
