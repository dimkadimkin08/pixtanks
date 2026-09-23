using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Linq;

public class OfficialServersLoader : MonoBehaviour
{

    private readonly List<ServerInfo> servers = new();

    [SerializeField]
    private string playerPrefsSelectedServerIdKey = "SelectedOfficialServerId";

    public ServerInfo[] Servers => servers.ToArray();
    public State CurrentState { get; private set; } = State.Success;
    public string CurrentServerId { get; private set; } = "";

    public event Action<State> OnStateChanged;
    public event Action<string> OnCurrentServerChanged;

    public enum State { Loading, Success, Failure }

    private void Awake()
    {
        CurrentServerId = PlayerPrefs.GetString(playerPrefsSelectedServerIdKey, "");
    }

    private void Start()
    {
#if !UNITY_EDITOR
        StartCoroutine(LoadServers());
#endif
    }

    private void OnDestroy()
    {
        OnStateChanged = null;
        OnCurrentServerChanged = null;
    }

    private IEnumerator LoadServers()
    {
        var errorWait = new WaitForSeconds(2);
        servers.Clear();
        while (true)
        {
            CurrentState = State.Loading;
            OnStateChanged?.Invoke(CurrentState);

            using UnityWebRequest request = UnityWebRequest.Get("https://pixtanks.online/api/servers");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    if (JsonUtility.FromJson<ServerListJson>(request.downloadHandler.text) is ServerListJson serverListJson)
                    {
                        foreach (var server in serverListJson.servers)
                        {
                            servers.Add(new ServerInfo
                            {
                                id = server.id,
                                name = server.name,
                                version = server.version,
                                address = server.address ?? "",
                                wsAddress = server.wsAddress ?? "",
                                playersCount = (ushort)server.playersCount,
                                playersLimit = (ushort)server.playersLimit,
                            });
                        }
                        CurrentState = State.Success;
                        OnStateChanged?.Invoke(CurrentState);
                        if (CurrentServerId == "" && servers.Count > 0)
                            SelectServer(servers[0].id);
                        break;
                    }
                }
                catch(Exception error)
                {
                    Debug.Log(error);
                }
            }
            else
            {
                Debug.Log(request.error);
            }
            CurrentState = State.Failure;
            OnStateChanged?.Invoke(CurrentState);
            yield return errorWait;
        }
    }

    public void Refresh()
    {
        if (CurrentState == State.Success)
        {
            StopAllCoroutines();
            StartCoroutine(LoadServers());
        }
    }

    public void SelectServer(string serverId)
    {
        if (CurrentState == State.Success)
        {
            CurrentServerId = serverId;
            PlayerPrefs.SetString(playerPrefsSelectedServerIdKey, serverId);
            OnCurrentServerChanged?.Invoke(CurrentServerId);
        }
    }

    public void JoinSelectedServer()
    {
        var networkManager = GameNetworkManager.Singleton;
        if (networkManager && CurrentState == State.Success && CurrentServerId != "" && servers.Any(server => server.id == CurrentServerId))
        {
            var server = servers.First(server => server.id == CurrentServerId);
#if UNITY_WEBGL
            networkManager.StartClientOfficialServer(server.wsAddress != "" ? server.wsAddress : server.address);
#else
            networkManager.StartClientOfficialServer(server.address != "" ? server.address : server.wsAddress);
#endif
        }
    }

    public struct ServerInfo
    {
        public string id;
        public string name;
        public string version;
        public string address;
        public string wsAddress;
        public ushort playersCount;
        public ushort playersLimit;
    }
    
    [Serializable]
    private class ServerItemJson
    {
        public string id;
        public string name;
        public string version;
        public string address;
        public string wsAddress;
        public int playersCount;
        public int playersLimit;
    }

    [Serializable]
    private class ServerListJson
    {
        public ServerItemJson[] servers;
    }
}