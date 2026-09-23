using UnityEngine;
public class GameMatchFlagIndicatorUI : MonoBehaviour
{

    [SerializeField]
    private NetworkCaptureFlag targetFlag;

    [Space]

    [SerializeField]
    private Transform directionDisplayTransform;
    [SerializeField]
    private float minDistance = 10;
    [SerializeField]
    private float maxDistance = 40;
    [SerializeField]
    private float minScale = 0.5f;
    [SerializeField]
    private float maxScale = 2f;

    [Space]

    [SerializeField]
    private GameObject directionIndicator;
    [SerializeField]
    private GameObject capturedIndicator;
    [SerializeField]
    private GameObject lostIndicator;

    private bool prevCaptured;
    private bool prevLost;

    private Camera _mainCamera;

    private void OnEnable()
    {
        _mainCamera = Camera.main;
        ForceUpdateIndicators();
    }

    private void Update()
    {
        Vector3 currentPos = _mainCamera ? _mainCamera.transform.position : transform.position;
        Vector3 dir = targetFlag.transform.position - currentPos;
        dir.z = 0;

        bool isTooClose = dir.magnitude < minDistance;
        bool isCaptured = !isTooClose && targetFlag.lastState == NetworkCaptureFlag.STATE_CAPTURED;
        bool isLost = !isTooClose && targetFlag.lastState == NetworkCaptureFlag.STATE_LOST;

        if (isCaptured != prevCaptured)
        {
            directionIndicator.SetActive(isCaptured || isLost);
            capturedIndicator.SetActive(isCaptured);
            prevCaptured = isCaptured;
        }

        if (isLost != prevLost)
        {
            directionIndicator.SetActive(isCaptured || isLost);
            lostIndicator.SetActive(isLost);
            prevLost = isLost;
        }

        directionDisplayTransform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        directionDisplayTransform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, Mathf.InverseLerp(minDistance, maxDistance, dir.magnitude));
    }

    private void ForceUpdateIndicators()
    {
        Vector3 currentPos = _mainCamera ? _mainCamera.transform.position : transform.position;
        Vector3 dir = targetFlag.transform.position - currentPos;
        dir.z = 0;
        bool isTooClose = dir.magnitude < minDistance;
        prevCaptured = !isTooClose && targetFlag.lastState == NetworkCaptureFlag.STATE_CAPTURED;
        prevLost = !isTooClose && targetFlag.lastState == NetworkCaptureFlag.STATE_LOST;

        capturedIndicator.SetActive(prevCaptured);
        lostIndicator.SetActive(prevLost);
        directionIndicator.SetActive(prevCaptured || prevLost);
    }
}