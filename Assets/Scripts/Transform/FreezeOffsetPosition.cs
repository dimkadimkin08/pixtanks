using UnityEngine;
public class FreezeOffsetPosition : MonoBehaviour
{
    public Vector3 globalOffsetPosition;

    private void FixedUpdate()
    {
        transform.localPosition = Vector3.zero;
        transform.position = transform.position + globalOffsetPosition;
    }

    private void Update()
    {
        transform.localPosition = Vector3.zero;
        transform.position = transform.position + globalOffsetPosition;
    }

    private void LateUpdate()
    {
        transform.localPosition = Vector3.zero;
        transform.position = transform.position + globalOffsetPosition;
    }
}
