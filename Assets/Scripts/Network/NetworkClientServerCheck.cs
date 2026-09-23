using UnityEngine;
using Mirror;
using UnityEngine.Events;
public class NetworkClientServerCheck : NetworkBehaviour
{

    [Header("Client")]
    [SerializeField]
    private UnityEvent onStartClient;
    [SerializeField]
    private UnityEvent onStartClientOnly;
    [Header("Server")]
    [SerializeField]
    private UnityEvent onStartServer;
    [SerializeField]
    private UnityEvent onStartServerOnly;

    public override void OnStartClient()
    {
        onStartClient?.Invoke();
        if (isClientOnly)
            onStartClientOnly?.Invoke();
    }

    public override void OnStartServer()
    {
        onStartServer?.Invoke();
        if (isClientOnly)
            onStartServerOnly?.Invoke();
    }
}
