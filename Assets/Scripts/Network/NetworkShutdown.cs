using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
public class NetworkShutdown : MonoBehaviour
{
    public static void ShutdownAll()
    {
        var wasClient = NetworkClient.active;
        if (GameNetworkManager.Singleton)
        {
            if (NetworkServer.activeHost)
                GameNetworkManager.Singleton.StopHost();
            else if (NetworkServer.active)
                GameNetworkManager.Singleton.StopServer();
            else if (NetworkClient.active)
                GameNetworkManager.Singleton.StopClient();
        }
        NetworkServer.DisconnectAll();
        NetworkServer.Shutdown();
        NetworkClient.Disconnect();
        NetworkClient.Shutdown();
        if (wasClient && NetworkManager.singleton.offlineScene != "")
        {
            SceneManager.LoadScene(NetworkManager.singleton.offlineScene);
        }
    }

    public void Shutdown()
    {
        ShutdownAll();
    }
}
