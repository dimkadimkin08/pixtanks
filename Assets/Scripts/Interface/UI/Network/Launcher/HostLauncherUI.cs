using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
public class HostLauncherUI : MonoBehaviour
{
    [SerializeField]
    private InputField portInput;
    [SerializeField]
    private InputField maxPlayersInput;
    [SerializeField]
    private InputField passwordInput;
    [SerializeField]
    private Toggle kickAfkToggle;
    [SerializeField]
    private InputField kickAfkTimeoutInput;
    [SerializeField]
    private Toggle commandsToggle;
    [SerializeField]
    private InputField adminPasswordInput;
    [SerializeField]
    private Slider worldSizeSlider;

    private void Start()
    {
        var configParams = GameNetworkManager.Singleton.serverConfig.GetConfigParams();
        portInput.text = configParams.port.ToString();
        passwordInput.text = configParams.serverPassword;
        maxPlayersInput.text = configParams.maxPlayers.ToString();
        kickAfkToggle.isOn = configParams.kickAfk;
        kickAfkTimeoutInput.text = configParams.kickAfkTimeout.ToString();
        commandsToggle.isOn = configParams.adminPasswordEnabled;
        adminPasswordInput.text = configParams.adminPassword.ToString();
        worldSizeSlider.minValue = NetworkServerConfig.ServerConfigParams.WorldSizeMinValue;
        worldSizeSlider.maxValue = NetworkServerConfig.ServerConfigParams.WorldSizeMaxValue;
        worldSizeSlider.value = configParams.worldSize;
    }

    public void StartHost()
    {
        var config = GameNetworkManager.Singleton.serverConfig;
        var configParams = config.GetConfigParams();
        configParams.port = portInput.text.Trim().TryParseNoLocale(out ushort portResult) ? portResult : config.defaults.port;
        configParams.maxPlayers = maxPlayersInput.text.Trim().TryParseNoLocale(out ushort maxPlayersResult) ? maxPlayersResult : config.defaults.maxPlayers;
        configParams.serverPassword = passwordInput.text.Trim();
        configParams.adminPasswordEnabled = commandsToggle.isOn;
        configParams.adminPassword = adminPasswordInput.text.Trim();
        configParams.kickAfk = kickAfkToggle.isOn;
        configParams.kickAfkTimeout = kickAfkTimeoutInput.text.Trim().TryParseNoLocale(out ushort timeoutResult) ? timeoutResult : config.defaults.kickAfkTimeout;
        configParams.worldSize = MathUShort.Clamp(Mathf.RoundToInt(worldSizeSlider.value));
        config.SetConfigParams(configParams);
        GameNetworkAuthenticator.ClientPassword = passwordInput.text;
        GameNetworkManager.Singleton.StartHostWithConfig();
    }
}
