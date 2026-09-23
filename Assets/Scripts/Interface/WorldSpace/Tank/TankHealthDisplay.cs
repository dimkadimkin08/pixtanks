using TMPro;
using UnityEngine;
public class TankHealthDisplay : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Health Display")]
    [SerializeField]
    private TextMeshPro healthText;
    [SerializeField]
    private Transform healthBarTransform;
    [SerializeField]
    private Vector3 healthBarFullPos = Vector3.zero;
    [SerializeField]
    private Vector3 healthBarEmptyPos = Vector3.zero;
    [SerializeField]
    private Vector3 healthBarFullScale = Vector3.one;
    [SerializeField]
    private Vector3 healthBarEmptyScale = Vector3.zero;

    private void Start()
    {
        ShowHealth(tank.Parameters.CurrentHp, tank.Parameters.MaxHp);
    }

    private void OnEnable()
    {
        tank.Parameters.OnCurrentHpSet += OnHealthChanged;
        tank.Parameters.OnMaxHpSet += OnMaxHealthChanged;
        ShowHealth(tank.Parameters.CurrentHp, tank.Parameters.MaxHp);
    }

    private void OnDisable()
    {
        tank.Parameters.OnCurrentHpSet -= OnHealthChanged;
        tank.Parameters.OnMaxHpSet -= OnMaxHealthChanged;
    }

    private void OnHealthChanged(ushort old, ushort current)
    {
        ShowHealth(current, tank.Parameters.MaxHp);
    }

    private void OnMaxHealthChanged(ushort old, ushort current)
    {
        ShowHealth(tank.Parameters.CurrentHp, current);
    }

    private void ShowHealth(int current, int max)
    {
        healthText.text = $"{current} / {max}";
        healthBarTransform.localPosition = Vector3.Lerp(healthBarEmptyPos, healthBarFullPos, current / (float)max);
        healthBarTransform.localScale = Vector3.Lerp(healthBarEmptyScale, healthBarFullScale, current / (float)max);
    }
}
