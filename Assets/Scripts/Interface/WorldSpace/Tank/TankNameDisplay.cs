using TMPro;
using UnityEngine;
public class TankNameDisplay : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Name Display")]
    [SerializeField]
    private TextMeshPro nameText;
    [SerializeField]
    private GameObject[] hideOnEmptyName;

    private void Start()
    {
        ShowDisplayName(tank.Parameters.DisplayName);
    }

    private void OnEnable()
    {
        tank.Parameters.OnDisplayNameSet += OnNameChanged;
        ShowDisplayName(tank.Parameters.DisplayName);
    }

    private void OnDisable()
    {
        tank.Parameters.OnDisplayNameSet -= OnNameChanged;
    }

    private void OnNameChanged(string old, string current)
    {
        ShowDisplayName(current);
    }

    private void ShowDisplayName(string displayName)
    {
        nameText.text = displayName;
        foreach (var obj in hideOnEmptyName)
            obj.SetActive(displayName.Length > 0);
    }
}
