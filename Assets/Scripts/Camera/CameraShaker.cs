using UnityEngine;
public class CameraShaker : MonoBehaviour
{
    public static Vector2 CameraPosition;

    public static float ShakeValue;

    public float shakeLimit = 10;
    public float shakeDeceleration = 2f;
    [Range(0f, 0.8f)]
    public float shakeEffectLoss = 0.4f;
    public CameraMovement cameraMovement;
    public Camera mainCamera;

    private float timeUntilChangePoint;

    private void Update()
    {
        CameraPosition = transform.position;
        if (ShakeValue == 0)
        {
            cameraMovement.shakeOffset = Vector2.zero;
            return;
        }
        ShakeValue = Mathf.Min(ShakeValue, shakeLimit);
        ShakeValue = Mathf.Lerp(ShakeValue, 0, shakeDeceleration * 0.75f * Time.deltaTime);
        ShakeValue = Mathf.MoveTowards(ShakeValue, 0, shakeDeceleration * Time.deltaTime);
        if (ShakeValue < 0)
        {
            cameraMovement.shakeOffset = Vector2.zero;
            return;
        }
        float adjustedShake = Mathf.Log(1 + ShakeValue) / Mathf.Log(1 + shakeLimit);
        adjustedShake *= 1 + Mathf.Lerp(mainCamera.orthographicSize / 2, 0, shakeEffectLoss);
        adjustedShake /= 9;
        timeUntilChangePoint -= Time.deltaTime / adjustedShake;
        if (timeUntilChangePoint <= 0)
        {
            cameraMovement.shakeOffset = Vector2.Lerp(adjustedShake * Random.insideUnitCircle, Vector2.zero, shakeEffectLoss);
        }
        else
        {
            cameraMovement.shakeOffset = Vector2.Lerp(cameraMovement.shakeOffset, Vector2.zero, adjustedShake * Time.deltaTime);
        }
    }

}