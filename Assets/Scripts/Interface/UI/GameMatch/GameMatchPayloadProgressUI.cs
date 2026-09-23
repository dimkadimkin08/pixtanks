using UnityEngine;
using UnityEngine.UI;
public class GameMatchPayloadProgressUI : MonoBehaviour
{

    [SerializeField]
    private NetworkEscortPayload targetPayload;

    [SerializeField]
    private Text progressText;
    [SerializeField]
    private Slider progressSlider;

    private void Update()
    {
        if (progressText)
            progressText.text = (Mathf.Round(targetPayload.Progress * 10000) / 100f).ToString("F2") + "%";
        if (progressSlider)
            progressSlider.value = targetPayload.Progress;
    }

}