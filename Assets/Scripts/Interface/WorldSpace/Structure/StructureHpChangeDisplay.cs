using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
public class StructureHpChangeDisplay : MonoBehaviour
{
    [Header("Structure")]
    [SerializeField]
    private NetworkStructure structure;

    [Header("Hp Change Text Effect")]
    [SerializeField]
    private GameObject hpTextPrefab;
    [SerializeField]
    private Vector2 spawnOffset;
    [SerializeField]
    private float randomSpawnRadius;

    private void OnEnable()
    {
        structure.OnCurrentHpSet += OnCurrentHpChanged;
    }

    private void OnDisable()
    {
        structure.OnCurrentHpSet -= OnCurrentHpChanged;
    }

    private void OnCurrentHpChanged(ushort old, ushort current)
    {
        var hpChange = current - old;
        if (hpChange > 0)
            ShowTextEffect(hpTextPrefab, $"+{hpChange}");
        if (hpChange < 0)
            ShowTextEffect(hpTextPrefab, $"{hpChange}");
    }

    private void ShowTextEffect(GameObject prefab, string text)
    {
        var spawnPosition = (Vector2)transform.position + spawnOffset + Random.insideUnitCircle * randomSpawnRadius;
        var numberObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        if (numberObject.TryGetComponent(out TextMeshPro textMeshPro))
            textMeshPro.text = text;
    }

}
