using UnityEngine;

public class GameMatchUIContainer : MonoBehaviour
{
    [SerializeField]
    private GameObject container;
    [SerializeField]
    private float maxDistance = 40f;

    private Camera mainCamera;

    private bool _wasInRange = false;

    private void OnEnable()
    {
        mainCamera = Camera.main;
        var isInRange = mainCamera && maxDistance > Vector2.Distance(mainCamera.transform.position, transform.position);
        _wasInRange = isInRange;
        container.SetActive(isInRange);
    }

    private void Update()
    {
        var isInRange = mainCamera && maxDistance > Vector2.Distance(mainCamera.transform.position, transform.position);
        if (_wasInRange != isInRange)
        {
            _wasInRange = isInRange;
            container.SetActive(isInRange);
        }
    }
}
