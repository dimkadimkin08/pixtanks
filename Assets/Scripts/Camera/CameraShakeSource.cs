using UnityEngine;
public class CameraShakeSource : MonoBehaviour
{

    [SerializeField]
    private float shakeValue;
    [SerializeField]
    private float maxDistanceToCamera = 20;

    [Space]

    [SerializeField]
    private bool shakeOnEnable;
    [SerializeField]
    private bool destroyOnEnable;

    private void OnEnable()
    {
        if (shakeOnEnable)
            ShakeCamera();
        if (destroyOnEnable)
            Destroy(gameObject);
    }

    public void ShakeCamera()
    {
        var distance = Vector2.Distance(transform.position, CameraShaker.CameraPosition);
        if (distance > maxDistanceToCamera)
            return;
        var multiplier = 1 - Mathf.Pow(distance / maxDistanceToCamera, 2);
        CameraShaker.ShakeValue += multiplier * shakeValue;
    }
}