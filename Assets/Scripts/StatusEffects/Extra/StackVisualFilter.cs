using UnityEngine;

public class StackVisualReader : MonoBehaviour
{
    [SerializeField] private StackingModifierStatusEffect effect;
    [SerializeField] private SpriteRenderer sr;

    [SerializeField] private Vector3 minScale = Vector3.one;
    [SerializeField] private Vector3 maxScale = Vector3.one * 2f;
    [SerializeField] private int maxStacks = 5;

    private void Update()
    {
        if (effect == null || sr == null)
            return;

        float t = (effect.Stacks - 1f) / (maxStacks - 1f);

        transform.localScale = Vector3.Lerp(minScale, maxScale, t);

        Color c = sr.color;
        c.a = Mathf.Lerp(0.3f, 1f, t);
        sr.color = c;
    }
}