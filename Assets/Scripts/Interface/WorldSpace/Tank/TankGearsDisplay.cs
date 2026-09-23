using TMPro;
using UnityEngine;
public class TankGearsDisplay : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Gears Display")]
    [SerializeField]
    private TextMeshPro gearsDisplayText;

    private void Start()
    {
        UpdateGearsDisplay();
    }

    private void OnEnable()
    {
        tank.Parameters.OnGearsSet += OnGearsChanged;
        UpdateGearsDisplay();
    }

    private void OnDisable()
    {
        tank.Parameters.OnGearsSet -= OnGearsChanged;
    }

    private void OnGearsChanged(ushort old, ushort current)
    {
        UpdateGearsDisplay();
    }

    private void UpdateGearsDisplay()
    {
        gearsDisplayText.text = tank.Parameters.Gears.ToString();
    }
}
