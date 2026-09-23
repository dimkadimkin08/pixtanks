using UnityEngine;
public class NetworkAutoServerLauncher : MonoBehaviour
{
    private void Start()
    {
        GameNetworkManager.Singleton.StartServerWithConfig();
    }
}
