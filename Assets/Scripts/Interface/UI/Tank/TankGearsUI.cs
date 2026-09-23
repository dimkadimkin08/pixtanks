using UnityEngine;
using UnityEngine.UI;
public class TankGearsUI : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Gears UI")]
    [SerializeField]
    private Text gearsCountText;

    private void Start()
    {
        if (tank)
            UpdateGears(tank.Parameters.Gears);
    }

    private void OnEnable()
    {
        if (!tank)
            return;
        tank.Parameters.OnGearsSet += OnGearsChanged;
        UpdateGears(tank.Parameters.Gears);
    }

    private void OnDisable()
    {
        if (!tank)
            return;
        tank.Parameters.OnGearsSet -= OnGearsChanged;
    }

    private void OnGearsChanged(ushort old, ushort current)
    {
        UpdateGears(current);
    }

    private void UpdateGears(int gears)
    {
        if (gearsCountText)
            gearsCountText.text = gears.ToString();
    }

    public void SetTank(NetworkTank newTank)
    {
        if (tank)
            tank.Parameters.OnGearsSet -= OnGearsChanged;
        tank = newTank;
        tank.Parameters.OnGearsSet += OnGearsChanged;
        UpdateGears(tank.Parameters.Gears);
    }
}
