using UnityEngine;
using UnityEngine.UI;
public class TankHealthUI : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Health UI")]
    [SerializeField]
    private Text healthText;
    [SerializeField]
    private Image healthBarImage;
    [SerializeField]
    private bool useTankTeamColor = true;

    private void Start()
    {
        if (tank)
            UpdateHealth();
    }

    private void OnEnable()
    {
        if (!tank)
            return;
        tank.Parameters.OnCurrentHpSet += OnHpChanged;
        tank.Parameters.OnMaxHpSet += OnMaxHpChanged;
        tank.Parameters.OnTeamIdSet += OnTeamIdChanged;
        UpdateHealth();
    }

    private void OnDisable()
    {
        if (!tank)
            return;
        tank.Parameters.OnCurrentHpSet -= OnHpChanged;
        tank.Parameters.OnMaxHpSet -= OnMaxHpChanged;
        tank.Parameters.OnTeamIdSet -= OnTeamIdChanged;
    }

    private void OnHpChanged(ushort old, ushort current)
    {
        UpdateHealth();
    }

    private void OnMaxHpChanged(ushort old, ushort current)
    {
        UpdateHealth();
    }

    private void OnTeamIdChanged(string old, string current)
    {
        UpdateTeamColor();
    }

    private void UpdateHealth()
    {
        if (!tank)
            return;
        var currentHp = tank.Parameters.CurrentHp;
        var maxHp = tank.Parameters.MaxHp;
        if (healthText)
            healthText.text = $"{currentHp} / {maxHp}";
        if (healthBarImage)
            healthBarImage.fillAmount = currentHp / (float)maxHp;
    }

    private void UpdateTeamColor()
    {
        if (!useTankTeamColor || !tank)
            return;
        var color = GameTanksManager.Singleton.GetTeam(tank.Parameters.TeamId)?.color ?? healthBarImage.color;
        healthBarImage.color = new Color(color.r, color.g, color.b, healthBarImage.color.a);
    }

    public void SetTank(NetworkTank newTank)
    {
        if (tank)
        {
            tank.Parameters.OnCurrentHpSet -= OnHpChanged;
            tank.Parameters.OnMaxHpSet -= OnMaxHpChanged;
            tank.Parameters.OnTeamIdSet -= OnTeamIdChanged;
        }
        tank = newTank;
        tank.Parameters.OnCurrentHpSet += OnHpChanged;
        tank.Parameters.OnMaxHpSet += OnMaxHpChanged;
        tank.Parameters.OnTeamIdSet += OnTeamIdChanged;
        UpdateHealth();
        UpdateTeamColor();
    }
}
