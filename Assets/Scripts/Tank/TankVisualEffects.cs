using System.Collections;
using UnityEngine;
public class TankVisualEffects : MonoBehaviour
{

    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Damage Effect (Child object to SetActive)")]
    [SerializeField]
    private GameObject damageEffectObject;
    [SerializeField]
    private float damageEffectDuration;

    [Header("Heal Effect (Child object to SetActive)")]
    [SerializeField]
    private GameObject healEffectObject;
    [SerializeField]
    private float healEffectDuration;

    [Header("Shoot Effects (Optional) (Child object to SetActive)")]
    [SerializeField]
    private GameObject[] shootEffectObjects;

    [Header("Reload Effects (Optional) (Child object to SetActive)")]
    [SerializeField]
    private GameObject[] reloadEffectObjects;

    [Header("Death Effect (Child object to retrieve)")]
    [SerializeField]
    private GameObject deathEffectObject;

    private void OnEnable()
    {
        tank.Parameters.OnCurrentHpSet += OnCurrentHpChanged;
        tank.Shoot.OnShoot += OnShoot;
        tank.Death.OnDeath += ShowDeathEffect;
    }

    private void OnDisable()
    {
        tank.Parameters.OnCurrentHpSet -= OnCurrentHpChanged;
        tank.Shoot.OnShoot -= OnShoot;
        tank.Death.OnDeath -= ShowDeathEffect;
    }

    private void OnCurrentHpChanged(ushort old, ushort current)
    {
        var hpChange = current - old;
        if (hpChange != 0)
            ShowHpChangeEffect(hpChange);
    }

    private void ShowHpChangeEffect(int hpChange)
    {
        StopCoroutine(nameof(ShowHealEffect));
        StopCoroutine(nameof(ShowDamageEffect));
        if (hpChange > 0)
            StartCoroutine(ShowHealEffect());
        else
            StartCoroutine(ShowDamageEffect());
    }

    private void OnShoot(TankShoot.PatternData patternData)
    {
        StartCoroutine(ShowShootEffects(patternData.index));
        StartCoroutine(ShowReloadEffects(patternData.index, patternData.nextIndex));
    }

    private void ShowDeathEffect()
    {
        if (deathEffectObject)
        {
            deathEffectObject.transform.parent = null;
            deathEffectObject.SetActive(true);
        }
    }

    private IEnumerator ShowShootEffects(int patternIndex)
    {
        GameObject effectObject = null;
        if (patternIndex < shootEffectObjects.Length && shootEffectObjects[patternIndex])
            effectObject = shootEffectObjects[patternIndex];
        if (effectObject)
        {
            effectObject.SetActive(false);
            effectObject.SetActive(true);
        }
        yield return new WaitWhile(() => tank.Shoot.PatternIndex == patternIndex && tank.Shoot.IsShooting);
        if (effectObject)
            effectObject.SetActive(false);
    }

    private IEnumerator ShowReloadEffects(int oldIndex, int nextPatternIndex)
    {
        if (reloadEffectObjects.Length > oldIndex && reloadEffectObjects[oldIndex])
        {
            reloadEffectObjects[oldIndex].SetActive(false);
        }
        if (reloadEffectObjects.Length > nextPatternIndex && reloadEffectObjects[nextPatternIndex])
        {
            reloadEffectObjects[nextPatternIndex].SetActive(true);
        }
        yield return new WaitWhile(() => tank.Shoot.PatternIndex == nextPatternIndex && tank.Shoot.IsReloading);
        if (reloadEffectObjects.Length > nextPatternIndex && reloadEffectObjects[nextPatternIndex])
        {
            reloadEffectObjects[nextPatternIndex].SetActive(false);
        }
    }

    private IEnumerator ShowHealEffect()
    {
        damageEffectObject.SetActive(false);
        healEffectObject.SetActive(false);
        healEffectObject.SetActive(true);
        yield return new WaitForSeconds(healEffectDuration);
        healEffectObject.SetActive(false);
    }

    private IEnumerator ShowDamageEffect()
    {
        damageEffectObject.SetActive(false);
        healEffectObject.SetActive(false);
        damageEffectObject.SetActive(true);
        yield return new WaitForSeconds(damageEffectDuration);
        damageEffectObject.SetActive(false);
    }
}
