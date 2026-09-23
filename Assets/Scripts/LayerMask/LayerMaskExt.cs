using UnityEngine;
public static class LayerMaskExt
{
    public static bool Contains(this LayerMask layermask, int layer) => layermask == (layermask | (1 << layer));
}
