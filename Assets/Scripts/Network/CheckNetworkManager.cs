using UnityEngine;
using UnityEngine.Events;
public class CheckNetworkManager : MonoBehaviour
{
    public UnityEvent onMissingNetworkManager;
    private void Start()
    {
        if (!GameNetworkManager.Singleton)
        {
            onMissingNetworkManager?.Invoke();
        }
    }
}