using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;

public class GameSettings : MonoBehaviour
{

    public const float MinInterfaceScale = 0.85f;
    public const float MaxInterfaceScale = 1.5f;
    public const float DefaultInterfaceScale = 1.1f;

    public const float MinChatScale = 0.5f;
    public const float MaxChatScale = 2.5f;
    public const float DefaultChatScale = 1f;

    public const int MinDesktopTargetFps = 30;
    public const int MaxDesktopTargetFps = 999;
    public const int DefaultDesktopTargetFps = 60;

    public const float MinMobileControlSize = 0.6f;
    public const float MaxMobileControlSize = 1.4f;
    public const float DefaultMobileControlSize = 1f;

    public static event Action<float> OnInterfaceScaleChanged;
    public static event Action<float> OnChatScaleChanged;
    public static event Action<float> OnMobileControlSizeChanged;

    public static bool MobileHighFpsEnabled
    {
        get
        {
            return PlayerPrefs.GetInt("MobileHighFpsEnabled", 1) > 0;
        }
        set
        {
            PlayerPrefs.SetInt("MobileHighFpsEnabled", value ? 1 : 0);
            if (!AppPlatform.IsHeadless && AppPlatform.IsMobile)
                Application.targetFrameRate = value ? 60 : 30;
        }
    }

    public static int DesktopTargetFps
    {
        get => PlayerPrefs.GetInt("DesktopTargetFps", DefaultDesktopTargetFps);
        set
        {
            var targetFps = Mathf.Clamp(value, MinDesktopTargetFps, MaxDesktopTargetFps);
            PlayerPrefs.SetInt("DesktopTargetFps", targetFps);
            if (!AppPlatform.IsHeadless && AppPlatform.IsDesktop && !AppPlatform.IsMobile)
                Application.targetFrameRate = targetFps;
        }
    }

    public static float CurrentAudioVolume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat("AudioVolume", 0.25f));
        set
        {
            var audioVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("AudioVolume", audioVolume);
            AudioListener.volume = audioVolume;
        }
    }

    public static float InterfaceScale
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat("InterfaceScale", DefaultInterfaceScale), MinInterfaceScale, MaxInterfaceScale);
        set
        {
            var interfaceScale = Mathf.Clamp(value, MinInterfaceScale, MaxInterfaceScale);
            PlayerPrefs.SetFloat("InterfaceScale", interfaceScale);
            OnInterfaceScaleChanged?.Invoke(interfaceScale);
        }
    }

    public static bool AltControlsEnabled
    {
        get => PlayerPrefs.GetInt("AltControlsEnabled", 0) > 0;
        set
        {
            PlayerPrefs.SetInt("AltControlsEnabled", value ? 1 : 0);
            NetworkPlayerInputsHandler.AltControlsEnabled = value;
        }
    }

    public static float MobileControlSize
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat("MobileControlSize", DefaultMobileControlSize), MinMobileControlSize, MaxMobileControlSize);
        set
        {
            var mobileControlSize = Mathf.Clamp(value, MinMobileControlSize, MaxMobileControlSize);
            PlayerPrefs.SetFloat("MobileControlSize", mobileControlSize);
            OnMobileControlSizeChanged?.Invoke(mobileControlSize);
        }
    }

    public static float ChatScale
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat("ChatScale", DefaultChatScale), MinChatScale, MaxChatScale);
        set
        {
            var chatScale = Mathf.Clamp(value, MinChatScale, MaxChatScale);
            PlayerPrefs.SetFloat("ChatScale", chatScale);
            OnChatScaleChanged?.Invoke(chatScale);
        }
    }

    public static float CurrentCameraZoom
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat("CameraZoomFloat", AppPlatform.IsMobile ? 0.25f : 0f));
        set
        {
            PlayerPrefs.SetFloat("CameraZoomFloat", Mathf.Clamp01(value));
            if (Camera.main && Camera.main.TryGetComponent(out CameraZoom cameraZoom))
                cameraZoom.SetZoom(Mathf.Clamp01(value));
        }
    }

    public static float DesktopTargetFpsFloat
    {
        get => DesktopTargetFps;
        set => DesktopTargetFps = Mathf.RoundToInt(value);
    }

    public static string DesktopTargetFpsString
    {
        get => DesktopTargetFps.ToString();
        set => DesktopTargetFps = value.TryParseNoLocale(out int result) ? result : DefaultDesktopTargetFps;
    }

    public static string InterfaceScaleString
    {
        get => (Mathf.RoundToInt(InterfaceScale * 100) / 100).ToString(CultureInfo.InvariantCulture);
        set => InterfaceScale = value.TryParseNoLocale(out float result) ? result : DefaultInterfaceScale;
    }

    public static string ChatScaleString
    {
        get => (Mathf.RoundToInt(ChatScale * 100) / 100).ToString(CultureInfo.InvariantCulture);
        set => ChatScale = value.TryParseNoLocale(out float result) ? result : DefaultChatScale;
    }

    public static string CurrentPlayerName
    {
        get
        {
            var playerName = PlayerPrefs.GetString("PlayerName", "").Trim();
            return playerName;
        }
        set
        {
            var playerName = value.Trim();
            PlayerPrefs.SetString("PlayerName", playerName);
        }
    }

    public static void SetLocale(int localeIndex)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];
    }

    public static void SetWindowedMode()
    {
        Screen.SetResolution(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2, FullScreenMode.Windowed);
    }

    public static void SetMaxWindowMode()
    {
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
    }

    public static void SetFullscreenMode()
    {
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.ExclusiveFullScreen);
    }

    [Header("Settings Limits Load Events")]
    public UnityEvent<float> onMinInterfaceScaleLoad;
    public UnityEvent<float> onMaxInterfaceScaleLoad;
    public UnityEvent<float> onMinChatScaleLoad;
    public UnityEvent<float> onMaxChatScaleLoad;
    public UnityEvent<int> onMinDesktopTargetFpsLoad;
    public UnityEvent<int> onMaxDesktopTargetFpsLoad;
    public UnityEvent<float> onMinMobileControlSizeLoad;
    public UnityEvent<float> onMaxMobileControlSizeLoad;
    [Header("Settings Load Events")]
    public UnityEvent<float> onCurrentAudioVolumeLoad;
    public UnityEvent<float> onInterfaceScaleLoad;
    public UnityEvent<float> onMobileControlSizeLoad;
    public UnityEvent<string> onInterfaceScaleStringLoad;
    public UnityEvent<float> onChatScaleLoad;
    public UnityEvent<string> onChatScaleStringLoad;
    public UnityEvent<bool> onMobileHighFpsLoad;
    public UnityEvent<float> onDesktopTargetFpsLoad;
    public UnityEvent<string> onDesktopTargetFpsStringLoad;
    public UnityEvent<float> onCameraZoomLoad;
    public UnityEvent<string> onPlayerNameLoad;
    public UnityEvent<bool> onAltControlsEnabledLoad;

    private void Start()
    {
        onMaxInterfaceScaleLoad?.Invoke(MaxInterfaceScale);
        onMinInterfaceScaleLoad?.Invoke(MinInterfaceScale);
        onMaxChatScaleLoad?.Invoke(MaxChatScale);
        onMinChatScaleLoad?.Invoke(MinChatScale);
        onMaxDesktopTargetFpsLoad?.Invoke(MaxDesktopTargetFps);
        onMinDesktopTargetFpsLoad?.Invoke(MinDesktopTargetFps);
        onMaxMobileControlSizeLoad?.Invoke(MaxMobileControlSize);
        onMinMobileControlSizeLoad?.Invoke(MinMobileControlSize);

        var currentAudioVolume = CurrentAudioVolume;
        var mobileHighFpsEnabled = MobileHighFpsEnabled;
        var desktopTargetFps = DesktopTargetFps;
        var interfaceScale = InterfaceScale;
        var mobileControlSize = MobileControlSize;
        var chatScale = ChatScale;
        var playerName = CurrentPlayerName;
        var altControls = AltControlsEnabled;

        CurrentAudioVolume = currentAudioVolume;
        MobileHighFpsEnabled = mobileHighFpsEnabled;
        DesktopTargetFps = desktopTargetFps;
        InterfaceScale = interfaceScale;
        MobileControlSize = mobileControlSize;
        ChatScale = chatScale;
        AltControlsEnabled = altControls;

        onCurrentAudioVolumeLoad?.Invoke(currentAudioVolume);
        onInterfaceScaleLoad?.Invoke(interfaceScale);
        onMobileControlSizeLoad?.Invoke(mobileControlSize);
        onChatScaleLoad?.Invoke(chatScale);
        onMobileHighFpsLoad?.Invoke(mobileHighFpsEnabled);
        onDesktopTargetFpsLoad?.Invoke(desktopTargetFps);
        onCameraZoomLoad?.Invoke(CurrentCameraZoom);
        onPlayerNameLoad?.Invoke(playerName);
        onInterfaceScaleStringLoad?.Invoke(InterfaceScaleString);
        onChatScaleStringLoad?.Invoke(ChatScaleString);
        onDesktopTargetFpsStringLoad?.Invoke(DesktopTargetFpsString);
        onAltControlsEnabledLoad?.Invoke(AltControlsEnabled);

        // I don't know why this setting sometimes doesn't apply on mobile devices, so I try to set it several times to make sure
        if (AppPlatform.IsMobile)
            StartCoroutine(ReapplyMobileFpsSetting());
    }

    private void OnEnable()
    {
        // I don't know why this setting sometimes doesn't apply on mobile devices, so I try to set it several times to make sure
        MobileHighFpsEnabled = MobileHighFpsEnabled;
    }

    private IEnumerator ReapplyMobileFpsSetting()
    {
        yield return new WaitForSeconds(1);
        MobileHighFpsEnabled = MobileHighFpsEnabled;
    }
}
