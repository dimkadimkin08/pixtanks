using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
public class MobileControlUISize : MonoBehaviour
{

    [SerializeField]
    private float minSizeValue = 0.5f;
    [SerializeField]
    private float maxSizeValue = 1.5f;

    [Header("Rect Transform")]
    [SerializeField]
    private RectTransform rectTransform;
    [Space]
    [SerializeField]
    private Vector2 minSizeAnchorMin = Vector2.one;
    [SerializeField]
    private Vector2 minSizeAnchorMax = Vector2.one;
    [Space]
    [SerializeField]
    private Vector2 maxSizeAnchorMin = Vector2.one;
    [SerializeField]
    private Vector2 maxSizeAnchorMax = Vector2.one;

    [Header("On Screen Stick (Optional)")]
    [SerializeField]
    private OnScreenStick stick;
    [SerializeField]
    private float minSitckRange = 75;
    [SerializeField]
    private float maxStickRange = 150;


    private void Start()
    {
        SetMobileControlSize(GameSettings.MobileControlSize);
    }

    private void OnEnable()
    {
        SetMobileControlSize(GameSettings.MobileControlSize);
        GameSettings.OnMobileControlSizeChanged += SetMobileControlSize;
    }

    private void OnDisable()
    {
        GameSettings.OnMobileControlSizeChanged -= SetMobileControlSize;
    }

    private void SetMobileControlSize(float size)
    {
        var lerpValue = (Mathf.Clamp(size, minSizeValue, maxSizeValue) - minSizeValue) / (maxSizeValue - minSizeValue);
        rectTransform.anchorMin = Vector2.Lerp(minSizeAnchorMin, maxSizeAnchorMin, lerpValue);
        rectTransform.anchorMax = Vector2.Lerp(minSizeAnchorMax, maxSizeAnchorMax, lerpValue);
        if (stick)
            stick.movementRange = Mathf.Lerp(minSitckRange, maxStickRange, lerpValue);
    }
}