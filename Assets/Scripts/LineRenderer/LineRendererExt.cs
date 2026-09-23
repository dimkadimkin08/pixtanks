using UnityEngine;
public static class LineRendererExt
{
	public static void SetStartColorKeepAlpha(this LineRenderer line, Color color)
	{
		var gradient = line.colorGradient;
		for (var i = 0; i < gradient.colorKeys.Length; i++)
			gradient.colorKeys[i].color = color;
		line.colorGradient = gradient;
		line.startColor = new Color(color.r, color.g, color.b, line.startColor.a);
		line.endColor = new Color(color.r, color.g, color.b, line.endColor.a);
	}
}
