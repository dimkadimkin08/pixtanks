using UnityEngine;
public class TiledSpriteSizeConstraint : MonoBehaviour
{

    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Transform scaleSource;
    [SerializeField]
    private Vector2 tiledSizeMultiplier;

    private void Update()
    {
        spriteRenderer.size = scaleSource.localScale * tiledSizeMultiplier;
    }
}
