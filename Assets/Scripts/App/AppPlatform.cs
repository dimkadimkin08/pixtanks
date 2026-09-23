using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
public static class AppPlatform
{
    public static RuntimePlatform Platform
    {
        get
        {
#if UNITY_ANDROID
            return RuntimePlatform.Android;
#elif UNITY_IOS
            return RuntimePlatform.IPhonePlayer;
#elif UNITY_STANDALONE_OSX
            return RuntimePlatform.OSXPlayer;
#elif UNITY_STANDALONE_WIN && !UNITY_SERVER
            return RuntimePlatform.WindowsPlayer;
#elif UNITY_STANDALONE_WIN && UNITY_SERVER
            return RuntimePlatform.WindowsServer;
#elif UNITY_STANDALONE_LINUX && !UNITY_SERVER
            return RuntimePlatform.LinuxPlayer;
#elif UNITY_STANDALONE_LINUX && UNITY_SERVER
            return RuntimePlatform.LinuxServer;
#elif UNITY_WEBGL
            return RuntimePlatform.WebGLPlayer;
#endif
        }
    }

    public static bool IsHeadless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    public static bool IsServer = Platform is RuntimePlatform.LinuxServer or RuntimePlatform.WindowsServer;
    public static bool IsDesktop = Platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.LinuxPlayer;
    public static bool IsWebGL = Platform is RuntimePlatform.WebGLPlayer;
    public static bool IsMobile = Platform is RuntimePlatform.Android || Application.isMobilePlatform;
}

