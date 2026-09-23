using UnityEngine;
using UnityEngine.Events;
public class SimpleDelayedEvent : MonoBehaviour
{
    public bool runOnAwake;
    public bool runOnStart;
    public bool runOnEnable;
    public float delay;
    public UnityEvent executeEvent;

    private bool _timerEnabled = false;
    private float _timer = 0;

    private void Awake()
    {
        if (runOnAwake)
            StartTimer();
    }

    private void Start()
    {
        if (runOnStart)
            StartTimer();
    }

    private void OnEnable()
    {
        if (runOnEnable)
            StartTimer();
    }

    private void Update()
    {
        if (!_timerEnabled)
            return;
        _timer += Time.deltaTime;
        if (_timer >= delay)
        {
            executeEvent?.Invoke();
            _timerEnabled = false;
        }
    }

    private void StartTimer()
    {
        _timer = 0;
        _timerEnabled = true;
    }
}