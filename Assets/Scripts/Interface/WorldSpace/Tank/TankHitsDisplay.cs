using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
public class TankHitsDisplay : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField]
    private NetworkTank tank;

    [Header("Local Player Hits")]
    [SerializeField]
    private GameObject localPlayerHitPosObject;
    [SerializeField]
    private UnityEvent OnLocalPlayerHit;
    [SerializeField]
    private UnityEvent OnLocalPlayerKill;

    [Header("Hits Value Text")]
    [SerializeField]
    private GameObject hitTextPrefab;
    [SerializeField]
    private GameObject localPlayerHitTextPrefab;
    [SerializeField]
    private Vector2 spawnOffset;
    [SerializeField]
    private float randomSpawnRadius;

    private void OnEnable()
    {
        tank.Parameters.ClientOnHitTaken += OnHitTaken;
    }

    private void OnDisable()
    {
        tank.Parameters.ClientOnHitTaken -= OnHitTaken;
    }

    private void OnHitTaken(TankParameters.HitData hitData)
    {
        var isLocalPlayer = hitData.fromPlayerNetId != null && NetworkTank.LocalPlayer && NetworkTank.LocalPlayer.netId == hitData.fromPlayerNetId;
        var text = hitData.hpValue > 0 ? "+" + hitData.hpValue.ToString() : hitData.hpValue.ToString();

        ShowTextEffect(localPlayerHitTextPrefab, text);

        if (isLocalPlayer)
        {
            localPlayerHitPosObject.transform.position = hitData.hitPos ?? transform.position;
            OnLocalPlayerHit?.Invoke();
            if (hitData.isKill)
                OnLocalPlayerKill?.Invoke();
        }
    }

    private void ShowTextEffect(GameObject prefab, string text)
    {
        var spawnPosition = (Vector2)transform.position + spawnOffset + Random.insideUnitCircle * randomSpawnRadius;
        var numberObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        if (numberObject.TryGetComponent(out TextMeshPro textMeshPro))
            textMeshPro.text = text;
    }

}
