using System;
using UnityEngine;

public class NetworkClientConfig : MonoBehaviour
{

    public ClientConfigParams defaults;

    [Space]

    public string playerPrefsAddressKey = "ClientAddress";
    public string playerPrefsPortKey = "ClientPort";

    public ClientConfigParams GetConfigParams()
    {
        try
        {
            return new()
            {
                address = PlayerPrefs.GetString(playerPrefsAddressKey, defaults.address),
                port = (ushort)PlayerPrefs.GetInt(playerPrefsPortKey, defaults.port),
            };
        }
        catch
        {
            SetConfigParams(defaults);
            return defaults;
        }
    }

    public void SetConfigParams(ClientConfigParams configParams)
    {
        PlayerPrefs.SetString(playerPrefsAddressKey, configParams.address);
        PlayerPrefs.SetInt(playerPrefsPortKey, configParams.port);
    }

    [Serializable]
    public struct ClientConfigParams
    {
        public string address;
        public ushort port;
    }
}

