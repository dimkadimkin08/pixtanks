using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ZapVisualBeam : MonoBehaviour
{
    public Transform startPoint;
    public Transform targetPoint;

    [Header("Zap Settings")]
    [Range(2, 64)] public int segments = 12;
    public float noiseStrength = 0.5f;
    public float noiseUpdateInterval = 0.05f;

    private LineRenderer lr;
    private float noiseTimer;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments;
    }

    private void OnEnable()
    {
        GenerateZap();
    }

    private void Update()
    {
        if (!startPoint || !targetPoint) return;

        noiseTimer += Time.deltaTime;

        if (noiseTimer >= noiseUpdateInterval)
        {
            noiseTimer = 0f;
            GenerateZap();
        }
        else
        {
            UpdateBaseLine();
        }
    }

    private void UpdateBaseLine()
    {
        lr.SetPosition(0, startPoint.position);
        lr.SetPosition(segments - 1, targetPoint.position);
    }

    private void GenerateZap()
    {
        lr.positionCount = segments;

        Vector3 start = startPoint.position;
        Vector3 end = targetPoint.position;

        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Random.onUnitSphere).normalized;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 basePos = Vector3.Lerp(start, end, t);

            if (i == 0 || i == segments - 1)
            {
                lr.SetPosition(i, basePos);
                continue;
            }

            float offset = Random.Range(-noiseStrength, noiseStrength);
            lr.SetPosition(i, basePos + perpendicular * offset);
        }
    }
}
