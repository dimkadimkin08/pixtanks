using UnityEngine;
public class URLOpener : MonoBehaviour
{
    public string[] urls;
    public void OpenURL(int urlIndex)
    {
        if (urls.Length > urlIndex && urlIndex >= 0)
            Application.OpenURL(urls[urlIndex]);
    }
}