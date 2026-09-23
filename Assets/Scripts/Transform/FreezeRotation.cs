using UnityEngine;
public class FreezeRotation : MonoBehaviour
{
    public Vector3 rotation;

    private void FixedUpdate()
    {
        transform.eulerAngles = rotation;
    }

    private void Update()
    {
        transform.eulerAngles = rotation;
    }

    private void LateUpdate()
    {
        transform.eulerAngles = rotation;
    }
}
