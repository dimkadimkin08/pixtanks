using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
public class ClientLauncherUI : MonoBehaviour
{
    [SerializeField]
    private InputField addressInput;
    [SerializeField]
    private InputField portInput;
    [SerializeField]
    private InputField passwordInput;

    private void OnEnable()
    {
        var configParams = GameNetworkManager.Singleton.clientConfig.GetConfigParams();
        addressInput.text = configParams.address;
        portInput.text = configParams.port.ToString();
    }

    public void StartClient()
    {
        var config = GameNetworkManager.Singleton.clientConfig;
        var addressTrimmed = addressInput.text.Trim();
        config.SetConfigParams(new NetworkClientConfig.ClientConfigParams
        {
            address = addressTrimmed.Length > 0 ? addressTrimmed : config.defaults.address,
            port = portInput.text.Trim().TryParseNoLocale(out ushort result) ? result : config.defaults.port
        });
        GameNetworkAuthenticator.ClientPassword = passwordInput.text;
        GameNetworkManager.Singleton.StartClientWithConfig();
    }
}
