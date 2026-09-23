using UnityEngine;
using UnityEngine.Rendering.Universal;
public class TankColorDisplay : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;
    [Header("Apply Team Color")]
    [SerializeField]
    private SpriteRenderer[] spriteRenderers;
    [SerializeField]
    private TrailRenderer[] trails;
    [SerializeField]
    private ParticleSystem[] particleSystems;

    private void Start()
    {
        ApplyTeamColor(tank.Parameters.TeamId);
    }

    private void OnEnable()
    {
        tank.Parameters.OnTeamIdSet += OnTeamIdChanged;
        ApplyTeamColor(tank.Parameters.TeamId);
    }

    private void OnDisable()
    {
        tank.Parameters.OnTeamIdSet -= OnTeamIdChanged;
    }

    private void OnTeamIdChanged(string old, string current)
    {
        ApplyTeamColor(current);
    }

    private void ApplyTeamColor(string teamId)
    {
        var team = GameTanksManager.Singleton.GetTeam(teamId);
        if (team == null)
            return;
        var color = team.color;
        foreach (var spriteRenderer in spriteRenderers)
            if (spriteRenderer)
                spriteRenderer.color = new Color(color.r, color.g, color.b, spriteRenderer.color.a);
        foreach (var particleSystemEffect in particleSystems)
            if (particleSystemEffect)
                particleSystemEffect.SetStartColorKeepAlpha(color);
        foreach (var trail in trails)
            if (trail)
                trail.SetStartColorKeepAlpha(color);
    }
}
