using UnityEngine;
using UnityEngine.UI;

public class GameMatchPayloadIndicatorUI : MonoBehaviour
{
    [SerializeField]
    private NetworkEscortPayload targetPayload;

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

    [SerializeField]
    private GameObject indicatorsContainer;

    [Space]

    [SerializeField]
    private GameObject forwardIndicator;
    [SerializeField]
    private GameObject backwardIndicator;
    [SerializeField]
    private GameObject idleIndicator;

    [Space]

    [SerializeField]
    private Text allyCountText;

    private bool _wasTooClose;
    private int _prevAllyCount;

    private Camera _mainCamera;

    private void OnEnable()
    {
        _wasTooClose = true;
        indicatorsContainer.SetActive(false);
        _mainCamera = Camera.main;
        ForceUpdateIndicators();
    }

    private void Update()
    {
        Vector3 currentPos = _mainCamera ? _mainCamera.transform.position : transform.position;
        Vector3 dir = targetPayload.transform.position - currentPos;
        dir.z = 0;

        bool isTooClose = dir.magnitude < minDistance;

        if (_wasTooClose != isTooClose)
        {
            indicatorsContainer.SetActive(!isTooClose);
            _wasTooClose = isTooClose;
        }

        int allyCount = targetPayload.allyTanksCount;

        if (allyCount != _prevAllyCount)
        {
            UpdateState(allyCount);
            _prevAllyCount = allyCount;
        }

        directionDisplayTransform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        directionDisplayTransform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, Mathf.InverseLerp(minDistance, maxDistance, dir.magnitude));
    }

    private void ForceUpdateIndicators()
    {
        _prevAllyCount = targetPayload.allyTanksCount;
        UpdateState(_prevAllyCount);
    }

    private void UpdateState(int allyCount)
    {
        forwardIndicator.SetActive(allyCount > 0);
        backwardIndicator.SetActive(allyCount < 0);
        idleIndicator.SetActive(allyCount == 0);

        allyCountText.text = allyCount > 0 ? allyCount.ToString() : string.Empty;
    }
}
