using TMPro;
using UnityEngine;
public class InterfaceTextMeshFontScale : MonoBehaviour
{
    [Header("Set font size based on GameSettings.InterfaceScale")]
    [SerializeField]
    private float baseFontSize;
    [SerializeField]
    private TextMeshPro text;
    [Header("or")]
    [SerializeField]
    private TMP_InputField input;

    private void Start()
    {
        SetFontScale(GameSettings.InterfaceScale);
    }

    private void OnEnable()
    {
        GameSettings.OnInterfaceScaleChanged += SetFontScale;
        SetFontScale(GameSettings.InterfaceScale);
    }

    private void OnDisable()
    {
        GameSettings.OnInterfaceScaleChanged -= SetFontScale;
    }

    private void SetFontScale(float fontScale)
    {
        if (text)
            text.fontSize = baseFontSize * fontScale;
        if (input)
            input.pointSize = baseFontSize * fontScale;
    }
}
