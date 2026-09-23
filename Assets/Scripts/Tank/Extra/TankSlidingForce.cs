using UnityEngine;
using Mirror;

public class TankSlidingForce : NetworkBehaviour
{
    [SerializeField] private NetworkTank _tank;
    [SerializeField] private float _moveSlideForce = 10f;
    [SerializeField] private float _velocityChangeSpeed = 0.5f;

    private Vector2 _velocity;

    private void FixedUpdate()
    {
        if (!isServer)
            return;
        _velocity = Vector2.Lerp(_velocity, _tank.Rigidbody.linearVelocity, Time.fixedDeltaTime * _velocityChangeSpeed);

        _tank.Movement.ServerAddForce(_velocity * _moveSlideForce * Time.fixedDeltaTime, bypassResistance: true);
    }
}