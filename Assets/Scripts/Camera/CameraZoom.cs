using UnityEngine;
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{

    [SerializeField]
    private float minSize = 10f;
    [SerializeField]
    private float maxSize = 16f;

    [HideInInspector]
    public float zoomOffset = 0;

    private float _targetZoom;
    private Camera _camera;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        SetZoom(GameSettings.CurrentCameraZoom);
    }

    private void Update()
    {
        _camera.orthographicSize = _targetZoom + zoomOffset;
    }

    public void SetZoom(float zoom)
    {
        _targetZoom = Mathf.Lerp(maxSize, minSize, Mathf.Clamp01(zoom));
        _camera.orthographicSize = _targetZoom + zoomOffset;
    }
}
