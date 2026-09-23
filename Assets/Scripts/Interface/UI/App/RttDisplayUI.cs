using System;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class RttDisplayUI : MonoBehaviour
{
    [SerializeField]
    private Text rttText;
    [SerializeField]
    private float updateInterval = 0.5f;

    private float _timeUntilUpdate;

    private void Update()
    {
        _timeUntilUpdate -= Time.deltaTime;
        if (_timeUntilUpdate > 0)
            return;
        _timeUntilUpdate = updateInterval;
        if (rttText)
            rttText.text = (int)Math.Round(NetworkTime.rtt * 1000) + " ms";
    }
}
