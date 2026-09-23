using UnityEngine;
public class NetworkHeadlessServerHandler : MonoBehaviour
{

    public static bool IsHeadless => AppPlatform.IsServer || AppPlatform.IsHeadless;

    [SerializeField]
    private bool handleOnAwake = true;
    [SerializeField]
    private bool handleOnStart;
    [SerializeField]
    private bool handleOnEnable;
    [SerializeField]
    private GameObject[] destroyOnHeadless;
    [SerializeField]
    private Behaviour[] disableOnHeadless;

    private void Awake()
    {
        if (handleOnAwake && IsHeadless)
            HandleHeadless();
    }

    private void Start()
    {
        if (handleOnStart && IsHeadless)
            HandleHeadless();
    }

    private void OnEnable()
    {
        if (handleOnEnable && IsHeadless)
            HandleHeadless();
    }

    private void HandleHeadless()
    {
        foreach (var script in disableOnHeadless)
            if (script)
                script.enabled = false;
        foreach (var obj in destroyOnHeadless)
            if (obj)
                Destroy(obj);
    }

}
