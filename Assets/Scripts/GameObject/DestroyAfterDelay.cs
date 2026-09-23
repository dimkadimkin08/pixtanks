using UnityEngine;
public class DestroyAfterDelay : MonoBehaviour
{

    [SerializeField]
    private float delay = 1;

    private float _timer = 0;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= delay)
            Destroy(gameObject);
    }

}
