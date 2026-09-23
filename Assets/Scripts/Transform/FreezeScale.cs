using UnityEngine;
public class FreezeScale : MonoBehaviour
{
    public Vector3 scale;

    private void FixedUpdate()
    {
        SetScale();
    }

    private void Update()
    {
        SetScale();
    }

    private void LateUpdate()
    {
        SetScale();
    }

    private void SetScale()
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(scale.x / transform.lossyScale.x, scale.y / transform.lossyScale.y, scale.z / transform.lossyScale.z);
    }
}
