using UnityEngine;
public class PlatformDependedObject : MonoBehaviour
{
    public bool allowOnMobile;
    public bool allowOnDesktop;
    public bool allowOnWebGL;
    public bool allowOnHeadless;
    [Tooltip("Prevent object from being active in any case on WebGL (for example, even in cases where isMobile is true and allowOnMobile is true)")]
    public bool preventOnWebGL;
    [Tooltip("Prevent object from being active in any case on Mobile (for example, even in cases where isWebGL is true and allowOnWebGL is true)")]
    public bool preventOnMobile;
    [Space]
    public OtherPlatformBehaviour onOtherPlatforms;

    public enum OtherPlatformBehaviour { Disable, Destroy }

    private void Awake()
    {
        if (!AppPlatform.IsHeadless || allowOnHeadless)
        {
            if ((!preventOnWebGL || !AppPlatform.IsWebGL) && (!preventOnMobile || !AppPlatform.IsMobile))
            {
                if (AppPlatform.IsHeadless && allowOnHeadless)
                    return;
                if (AppPlatform.IsDesktop && allowOnDesktop)
                    return;
                if (AppPlatform.IsWebGL && allowOnWebGL)
                    return;
                if (AppPlatform.IsMobile && allowOnMobile)
                    return;
            }
        }
        switch (onOtherPlatforms)
        {
            case OtherPlatformBehaviour.Disable:
                gameObject.SetActive(false);
                break;
            case OtherPlatformBehaviour.Destroy:
                Destroy(gameObject);
                break;
        }
    }
}
