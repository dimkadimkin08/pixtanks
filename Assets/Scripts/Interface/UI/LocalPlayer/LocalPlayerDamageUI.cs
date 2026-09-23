using UnityEngine;
using UnityEngine.UI;
using static GameTanksManager;
public class LocalPlayerDamageUI : MonoBehaviour
{

    [Header("Local Player Damage Effect")]
    [SerializeField]
    private Image damageEffectImage;
    [SerializeField]
    private Image healEffectImage;
    [SerializeField]
    private float alphaDeceleration = 2;
    [SerializeField, Range(0, 1)]
    private float maxAlpha = 0.75f;
    [SerializeField, Range(0, 1)]
    private float maxDamage = 0.5f;

    private NetworkTank tank;

    private void OnEnable()
    {
        NetworkTank.OnLocalPlayerSpawn += OnLocalPlayerSpawn;
        NetworkTank.OnLocalPlayerDestroyed += OnLocalPlayerDestroyed;
        if (NetworkTank.LocalPlayer)
            OnLocalPlayerSpawn(NetworkTank.LocalPlayer);
    }

    private void OnDisable()
    {
        NetworkTank.OnLocalPlayerSpawn -= OnLocalPlayerSpawn;
        NetworkTank.OnLocalPlayerDestroyed -= OnLocalPlayerDestroyed;
    }

    private void Update()
    {
        if (damageEffectImage.color.a == 0 && healEffectImage.color.a == 0)
            return;
        float damageAlpha = Mathf.Lerp(damageEffectImage.color.a, 0, alphaDeceleration * Time.deltaTime);
        damageEffectImage.color = new Color(damageEffectImage.color.r, damageEffectImage.color.g, damageEffectImage.color.b, damageAlpha);
        float healAlpha = Mathf.Lerp(healEffectImage.color.a, 0, alphaDeceleration * Time.deltaTime);
        healEffectImage.color = new Color(healEffectImage.color.r, healEffectImage.color.g, healEffectImage.color.b, healAlpha);
    }

    private void OnLocalPlayerSpawn(NetworkTank localPlayerTank)
    {
        tank = localPlayerTank;
        tank.Parameters.OnCurrentHpSet += OnLocaPlayerHpChanged;
    }

    private void OnLocalPlayerDestroyed()
    {
        damageEffectImage.color = new Color(damageEffectImage.color.r, damageEffectImage.color.g, damageEffectImage.color.b, 0);
        healEffectImage.color = new Color(healEffectImage.color.r, healEffectImage.color.g, healEffectImage.color.b, 0);
    }

    private void OnLocaPlayerHpChanged(ushort old, ushort current)
    {
        if (!tank || current == 0)
            return;
        var value = current - old;
        if (value < 0)
        {
            float maxHp = tank.Parameters.MaxHp;
            float alpha = maxAlpha * Mathf.Clamp01(-value / maxHp / maxDamage);
            damageEffectImage.color = new Color(damageEffectImage.color.r, damageEffectImage.color.g, damageEffectImage.color.b, alpha);
        }
        if (value > 0)
        {
            float maxHp = tank.Parameters.MaxHp;
            float alpha = maxAlpha * Mathf.Clamp01(value / maxHp / maxDamage);
            healEffectImage.color = new Color(healEffectImage.color.r, healEffectImage.color.g, healEffectImage.color.b, alpha);
        }
    }
}
