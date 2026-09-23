using UnityEngine;
public static class Rigidbody2DExt
{
    /// <summary>
    /// Checks the global Physics2D collision layer settings and the local Rigidbody collision layer settings to calculate the physics layer mask for this Rigidbody's layer.
    /// (This is an expensive function! Avoid using it in Update-like functions. Instead cache the results if you need to use them frequently.)
    /// </summary>
    /// <param name="rb">This Rigidbody2D</param>
    /// <returns></returns>
    public static LayerMask CalculateLayerMask(this Rigidbody2D rb)
    {
        var currentLayer = rb.gameObject.layer;
        var finalMask = rb.includeLayers;
        for (ushort i = 0; i < 32; i++)
            if ((rb.excludeLayers & 1 << i) == 0 && !Physics2D.GetIgnoreLayerCollision(currentLayer, i))
                finalMask |= 1 << i;
        return finalMask;
    }
}
