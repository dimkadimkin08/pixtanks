using System;
using System.Net;
using System.Threading;
using Mirror;
using UnityEngine;

public class HeaddlessOfficialServerApiHost : MonoBehaviour
{
    private HttpListener httpListener;
    private Thread listenerThread;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        listenerThread = new Thread(StartListener);
        listenerThread.Start();
    }

    private void OnApplicationQuit()
    {
        httpListener?.Stop();
        listenerThread?.Join();
    }

    private void StartListener()
    {
        //var configParams = GameNetworkManager.Singleton.serverConfig.GetConfigParams();
        //if (configParams.officialServerApiPort <= 0)
        //{
        //    return;
        //}
        //if (configParams.port == configParams.officialServerApiPort || configParams.headlessLocalCommandsPort == configParams.officialServerApiPort)
        //{
        //    Debug.Log($"HTTP official server api port conflicts with other ports in the config. Cannot start local commands server.");
        //    return;
        //}
        //httpListener = new HttpListener();
        //httpListener.Prefixes.Add($"http://127.0.0.1:{configParams.officialServerApiPort}/");
        //httpListener.Start();

        //Debug.Log($"HTTP official server api server started on http://127.0.0.1:{configParams.officialServerApiPort}");

        //while (true)
        //{
        //    HttpListenerContext context = httpListener.GetContext();
        //    HttpListenerRequest request = context.Request;
        //    HttpListenerResponse response = context.Response;

        //    var playersCount = 0;
        //    var playersLimit = 0;
        //    try
        //    {
        //        playersCount = GameNetworkManager.Singleton.numPlayers;
        //        playersLimit = GameNetworkManager.Singleton.maxConnections;
        //    }
        //    catch
        //    {

        //    }
        //    var data = new ServerDataResponse
        //    {
        //        serverOnline = NetworkServer.active,
        //        serverVersion = Application.version,
        //        serverPlayersCount = playersCount,
        //        serverPlayersLimit = playersLimit,
        //    };
        //    var output = JsonUtility.ToJson(data);

        //    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(output);
        //    response.ContentLength64 = buffer.Length;
        //    response.OutputStream.Write(buffer, 0, buffer.Length);
        //    response.OutputStream.Close();
        //}
    }

    [Serializable]
    private class ServerDataResponse
    {
        public bool serverOnline;
        public string serverVersion;
        public int serverPlayersCount;
        public int serverPlayersLimit;
    }
}