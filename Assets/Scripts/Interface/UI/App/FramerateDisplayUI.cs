using UnityEngine;
using UnityEngine.UI;

public class FramerateDisplayUI : MonoBehaviour
{

    [SerializeField]
    private Text fpsText;

    [Space]

    [SerializeField]
    private float updateInterval = 0.5f;

    private int _frames;
    private float _timeUntilUpdate;

    private void Update()
    {
        _frames++;
        _timeUntilUpdate -= Time.deltaTime;
        if (_timeUntilUpdate > 0)
            return;
        _timeUntilUpdate = updateInterval;
        if (fpsText)
            fpsText.text = Mathf.FloorToInt(_frames / updateInterval) + " fps";
        _frames = 0;
    }

}
