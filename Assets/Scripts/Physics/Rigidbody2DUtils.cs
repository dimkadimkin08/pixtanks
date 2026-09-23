using UnityEngine;
public static class Rigidbody2DUtils
{
    /// <summary>
    /// Checks the global Physics2D collision layer settings and the local Rigidbody collision layer settings to calculate the physics layer mask for this Rigidbody's layer.
    /// (This is an expensive function! Avoid using it in Update-like functions. Instead cache the results if you need to use them frequently.)
    /// </summary>
    /// <param name="rb">This Rigidbody2D</param>
    /// <returns></returns>
    public static LayerMask CalculateLayerMask(int layer)
    {
        var finalMask = 0;
        for (ushort i = 0; i < 32; i++)
            if (!Physics2D.GetIgnoreLayerCollision(layer, i))
                finalMask |= 1 << i;
        return finalMask;
    }
}
