using UnityEngine;
public static class TrailExt
{
    public static void SetStartColorKeepAlpha(this TrailRenderer trail, Color color)
    {
        var gradient = trail.colorGradient;
        for (var i = 0; i < gradient.colorKeys.Length; i++)
            gradient.colorKeys[i].color = color;
        trail.colorGradient = gradient;
        trail.startColor = new Color(color.r, color.g, color.b, trail.startColor.a);
        trail.endColor = new Color(color.r, color.g, color.b, trail.endColor.a);
    }
}
