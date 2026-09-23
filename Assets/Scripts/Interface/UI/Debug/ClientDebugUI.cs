using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ClientDebugUI : MonoBehaviour
{
    private static ClientDebugUI instance;

    public static bool IsDebugUIEnabled = false;

    [SerializeField] private bool dontDestroyOnLoad = true;

    [SerializeField] private GameObject uiContainer;

    [SerializeField] private Text generalDebugField;

    private float fpsTimer;
    private int fpsFrames;
    private float fps;

    private bool _wasVisible = false;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            instance = this;
        }
    }

    private void OnEnable()
    {
        _wasVisible = IsDebugUIEnabled;
        uiContainer.SetActive(_wasVisible);
    }

    private void Update()
    {
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            var debugEnabled = TaknDebugDisplay.IsDebugDisplayEnabled && IsDebugUIEnabled;
            debugEnabled = !debugEnabled;

            TaknDebugDisplay.IsDebugDisplayEnabled = debugEnabled;
            IsDebugUIEnabled = debugEnabled;
        }

        if (_wasVisible != IsDebugUIEnabled)
        {
            _wasVisible = IsDebugUIEnabled;
            uiContainer.SetActive(_wasVisible);
        }

        if (_wasVisible)
        {
            UpdateFPS();
            UpdateGeneralDebug();
        }
    }

    private void UpdateFPS()
    {
        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= 1f)
        {
            fps = fpsFrames / fpsTimer;
            fpsFrames = 0;
            fpsTimer = 0;
        }
    }

    private void UpdateGeneralDebug()
    {
        if (!generalDebugField) return;

        var uptime = DateTime.UtcNow - LogCollector.ServerStartTime;

        long memory = GC.GetTotalMemory(false);

        int logs = LogCollector.Count(LogType.Log);
        int warnings = LogCollector.Count(LogType.Warning);
        int errors = LogCollector.Count(LogType.Error);
        int exceptions = LogCollector.Count(LogType.Exception);

        StringBuilder sb = new();

        sb.AppendLine("DEBUG INFO (F3 toggle)");
        sb.AppendLine("-------------------------");
        sb.AppendLine($"Uptime: {uptime:dd\\.hh\\:mm\\:ss}");
        sb.AppendLine($"FPS: {fps:F1}");
        sb.AppendLine($"Memory: {memory / (1024 * 1024)} MB");

        sb.AppendLine();

        sb.AppendLine($"Platform: {Application.platform}");
        sb.AppendLine($"Unity: {Application.unityVersion}");
        sb.AppendLine($"Resolution: {Screen.width}x{Screen.height}");
        sb.AppendLine($"Target FPS: {Application.targetFrameRate}");

        sb.AppendLine();
        sb.AppendLine($"Logs: {logs}");
        sb.AppendLine($"Warnings: {warnings}");
        sb.AppendLine($"Errors: {errors}");
        sb.AppendLine($"Exceptions: {exceptions}");

        generalDebugField.text = sb.ToString();
    }
}