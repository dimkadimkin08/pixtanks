using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class TankAimPointDisplay : MonoBehaviour
{
    [SerializeField]
    private NetworkTank tank;

    [SerializeField]
    private List<PatternParams> patternParams = new();

    private void Update()
    {
        var patternIndex = tank.Shoot.PatternIndex;
        var currentParams = patternParams.Count > patternIndex ? patternParams[patternIndex] : default;
        Vector3 originPosition = currentParams.origin.position;
        Vector3 originUp = currentParams.origin.up;
        Vector3 originRight = currentParams.origin.right;
        var offset = currentParams.originOffset.y * originUp + currentParams.originOffset.x * originRight;
        var hits = Physics2D.CircleCastAll(
            originPosition + offset,
            currentParams.attackRadius,
            (Vector2)originUp,
            currentParams.maxDistance,
            currentParams.layerMask
        ).Where(hit => hit.collider.attachedRigidbody != tank.Rigidbody).ToArray();
        var distance = hits.Length > 0 ? hits[0].distance : currentParams.maxDistance;
        transform.position = originPosition + offset + originUp * distance;
    }

    [System.Serializable]
    public class PatternParams
    {
        public Transform origin;
        public Vector2 originOffset = Vector2.zero;
        public float attackRadius = 0.15f;
        public float maxDistance = 10;
        public LayerMask layerMask = 0;
    }
}