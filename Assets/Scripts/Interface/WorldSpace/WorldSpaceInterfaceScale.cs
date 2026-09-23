using UnityEngine;
public class WorldSpaceInterfaceScale : MonoBehaviour
{
    [SerializeField]
    private Vector2 defaultScale = Vector2.one;
    [SerializeField]
    private float scaleSensitivity = 1;
    [SerializeField]
    private float minScale = 0.5f;
    [SerializeField]
    private float maxScale = 1.5f;

    private void Start()
    {
        SetInterfaceScale(GameSettings.InterfaceScale);
    }

    private void OnEnable()
    {
        SetInterfaceScale(GameSettings.InterfaceScale);
        GameSettings.OnInterfaceScaleChanged += SetInterfaceScale;
    }

    private void OnDisable()
    {
        GameSettings.OnInterfaceScaleChanged -= SetInterfaceScale;
    }

    private void SetInterfaceScale(float scale)
    {
        transform.localScale = defaultScale * Mathf.Clamp(1 + (scale - 1) * scaleSensitivity, minScale, maxScale);
    }
}
