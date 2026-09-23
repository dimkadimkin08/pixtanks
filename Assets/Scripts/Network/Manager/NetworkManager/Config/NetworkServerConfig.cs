using System;
using System.IO;
using UnityEngine;
public class NetworkServerConfig : MonoBehaviour
{
    public ServerConfigParams defaults;

    public string configFileName = "server-config.json";

    public string FilePath => $"{Directory.GetCurrentDirectory()}/{configFileName}";

    public ServerConfigParams GetConfigParams()
    {
        try
        {
            return JsonUtility.FromJson<ServerConfigParams>(File.ReadAllText(FilePath)).Format();
        }
        catch
        {
            SetConfigParams(defaults);
            return defaults;
        }
    }

    public void SetConfigParams(ServerConfigParams configParams)
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(configParams.Format(), true));
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    [Serializable]
    public struct ServerConfigParams
    {
        public const ushort MaxPlayersMinValue = 1;
        public const ushort MaxPlayersMaxValue = 100;

        public const ushort KickAfkTimoutMinValue = 30;
        public const ushort KickAfkTimeoutMaxValue = 600;

        public const ushort WorldSizeMinValue = 1;
        public const ushort WorldSizeMaxValue = 3;

        public ServerConfigParams Format()
        {
            maxPlayers = Math.Clamp(maxPlayers, MaxPlayersMinValue, MaxPlayersMaxValue);
            kickAfkTimeout = Math.Clamp(kickAfkTimeout, KickAfkTimoutMinValue, KickAfkTimeoutMaxValue);
            worldSize = Math.Clamp(worldSize, WorldSizeMinValue, WorldSizeMaxValue);
            return this;
        }

        [Range(1, 3)]
        public ushort worldSize;

        [Space]

        public ushort port;

        [Space]

        public string serverPassword;

        [Space]

        public bool adminPasswordEnabled;
        public string adminPassword;

        [Space]

        [Range(MaxPlayersMinValue, MaxPlayersMaxValue)]
        public ushort maxPlayers;

        [Space]

        public bool kickAfk;
        [Range(KickAfkTimoutMinValue, KickAfkTimeoutMaxValue)]
        public ushort kickAfkTimeout;

        [Space]

        public string serverChatLogFile;

    }
}

