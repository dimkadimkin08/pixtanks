using UnityEngine;
using UnityEngine.UI;
public class VersionDisplayUI : MonoBehaviour
{
    [SerializeField]
    private Text versionText;

    private void Start()
    {
        if (versionText)
            versionText.text = "v" + Application.version;
    }
}
