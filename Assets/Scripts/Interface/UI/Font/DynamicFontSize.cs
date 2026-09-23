using UnityEngine;

[System.Serializable]
public class DynamicFontSize
{
    public float minPixels = 400;
    public int minFontSize = 4;
    [Space]
    public float maxPixels = 1500;
    public int maxFontSize = 24;

    public int GetFontSize()
    {
        var pixels = (Screen.height + Screen.width) / 2f;
        return Mathf.RoundToInt(Mathf.Lerp(minFontSize, maxFontSize, (pixels - minPixels) / (maxPixels - minPixels)));
    }
}
