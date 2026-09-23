using System.Collections;
using UnityEngine;
public class GuideUI : MonoBehaviour
{
    [SerializeField]
    private GameObject containerToEnable;
    [SerializeField]
    private float destroyAfterDelay;

    private bool isActivated;

    private void OnEnable()
    {
        NetworkTank.OnLocalPlayerSpawn += OnLocalPlayerSpawn;
    }

    private void OnDisable()
    {
        NetworkTank.OnLocalPlayerSpawn -= OnLocalPlayerSpawn;
    }

    private void OnLocalPlayerSpawn(NetworkTank tank)
    {
        if (isActivated)
            return;
        isActivated = true;
        containerToEnable.SetActive(true);
        StartCoroutine(DestoryAfterDelay());
    }

    private IEnumerator DestoryAfterDelay()
    {
        yield return new WaitForSeconds(destroyAfterDelay);
        Destroy(gameObject);
    }
}