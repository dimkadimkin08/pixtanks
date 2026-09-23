using UnityEngine;
using UnityEngine.Events;
public class PlatformDependedEvent : MonoBehaviour
{
    public bool preventOnMobile;
    public bool preventOnDesktop;
    public bool preventOnWebGL;
    public bool preventOnHeadless;
    [Tooltip("Do event in any case on WebGL (for example, even in cases where isMobile is true and allowOnMobile is true)")]
    public bool doOnWebGL;
    [Tooltip("Do event in any case on Mobile (for example, even in cases where isWebGL is true and allowOnWebGL is true)")]
    public bool doOnMobile;
    [Space]
    public UnityEvent doEvent;

    private void Awake()
    {
        if (!AppPlatform.IsHeadless || !preventOnHeadless)
        {
            if ((!doOnWebGL || !AppPlatform.IsWebGL) && (!doOnMobile || !AppPlatform.IsMobile))
            {
                if (AppPlatform.IsHeadless && preventOnHeadless)
                    return;
                if (AppPlatform.IsDesktop && preventOnDesktop)
                    return;
                if (AppPlatform.IsWebGL && preventOnWebGL)
                    return;
                if (AppPlatform.IsMobile && preventOnMobile)
                    return;
            }
        }
        doEvent?.Invoke();
    }
}
