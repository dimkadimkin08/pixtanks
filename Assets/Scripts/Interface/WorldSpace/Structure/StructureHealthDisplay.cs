using TMPro;
using UnityEngine;
public class StructureHealthDisplay : MonoBehaviour
{
    [Header("Structure")]
    [SerializeField]
    private NetworkStructure structure;

    [Header("Health Display")]
    [SerializeField]
    private TextMeshPro healthText;

    private void Start()
    {
        ShowHealth(structure.CurrentHp, structure.MaxHp);
    }

    private void OnEnable()
    {
        structure.OnCurrentHpSet += OnHealthChanged;
        structure.OnMaxHpSet += OnMaxHealthChanged;
        ShowHealth(structure.CurrentHp, structure.MaxHp);
    }

    private void OnDisable()
    {
        structure.OnCurrentHpSet -= OnHealthChanged;
        structure.OnMaxHpSet -= OnMaxHealthChanged;
    }

    private void OnHealthChanged(ushort old, ushort current)
    {
        ShowHealth(current, structure.MaxHp);
    }

    private void OnMaxHealthChanged(ushort old, ushort current)
    {
        ShowHealth(structure.CurrentHp, current);
    }

    private void ShowHealth(int current, int max)
    {
        healthText.text = $"{current} / {max}";
    }
}
