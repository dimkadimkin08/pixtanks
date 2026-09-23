using Mirror;
using UnityEngine;
using UnityEngine.Events;
public class NetworkMapButtonTrigger : MonoBehaviour
{
    public UnityEvent serverOnTrigger;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!NetworkServer.active)
            return;
        var rigidbody = collision.attachedRigidbody;
        if (!rigidbody)
            return;
        if (!rigidbody.TryGetComponent(out NetworkTank tank))
            return;
        serverOnTrigger?.Invoke();
    }
}