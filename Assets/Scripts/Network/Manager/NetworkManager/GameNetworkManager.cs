using kcp2k;
using Mirror;
using Mirror.SimpleWeb;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameNetworkManager : NetworkManager
{

    public static string NetworkServerPassword = "";

    private bool IsErrorState => ClientState is NetworkClientState.ConnectionFailed or NetworkClientState.AuthFailed or NetworkClientState.Error or NetworkClientState.Disconnected;

    public static GameNetworkManager Singleton => singleton as GameNetworkManager;

    public static NetworkClientState ClientState { get; private set; } = NetworkClientState.None;
    public static string ClientStateErrorMessage;

    [Header("Interest")]
    public DistanceInterestManagement interestManagement;

    [Header("Managers")]
    public GameTanksManager tanksManager;

    [Header("Configuration")]
    public NetworkClientConfig clientConfig;
    public NetworkServerConfig serverConfig;
    private string OnlineSceneName => onlineScene[(onlineScene.LastIndexOf('/') + 1)..];
    private string CurrentSceneName => SceneManager.GetActiveScene().name;

    [HideInInspector]
    public double ClientLastTeamChoiceKickTime;

    // conn id to request time
    private readonly Dictionary<int, double> serverPlayerTeamChoiceRequestTimestamps = new();

    public readonly NetworkPlayerInputsHandler PlayerInputsHandler = new();

    public event Action<NetworkConnectionToClient> ServerOnPlayerJoin;
    public event Action<NetworkConnectionToClient> ServerOnPlayerLeave;

    public event Action<NetworkClientState> OnClientStateSet;

    public enum NetworkClientState { None, Connecting, Loading, Playing, ConnectionFailed, AuthFailed, Error, Disconnected }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ServerOnPlayerJoin = null;
        ServerOnPlayerLeave = null;
        OnClientStateSet = null;
    }

    public override void OnStartServer()
    {
        if (authenticator)
            authenticator.OnServerAuthenticated.AddListener(OnServerAuthenticated);
        PlayerInputsHandler.OnStartServer();
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        tanksManager.RequestPlayerSpawn(conn);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {

    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"Player disconnect: connId: {conn.connectionId}; address: {conn.address}; name: {conn.authenticationData}");
        base.OnServerDisconnect(conn);
        if (conn.isAuthenticated)
            ServerOnPlayerLeave?.Invoke(conn);
        PlayerInputsHandler.OnServerDisconnect(conn);
        if (serverPlayerTeamChoiceRequestTimestamps.ContainsKey(conn.connectionId))
            serverPlayerTeamChoiceRequestTimestamps.Remove(conn.connectionId);
        tanksManager.OnPlayerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        if (authenticator)
            authenticator.OnServerAuthenticated.RemoveListener(OnServerAuthenticated);
        PlayerInputsHandler.OnStopServer();
        if (ClientState is NetworkClientState.Connecting or NetworkClientState.Loading)
            SetClientState(NetworkClientState.None);
    }

    public override void OnStartClient()
    {
        if (authenticator)
            authenticator.OnClientAuthenticated.AddListener(OnClientAuthenticated);
        if (!IsErrorState)
            SetClientState(NetworkClientState.Connecting);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnServerError(NetworkConnectionToClient conn, TransportError error, string reason)
    {
        base.OnServerError(conn, error, reason);
        conn?.Disconnect();
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
        if (!IsErrorState && NetworkClient.active && newSceneName == OnlineSceneName)
            SetClientState(NetworkClientState.Loading);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        if (NetworkClient.connection.isAuthenticated)
            SetClientState(NetworkClientState.Playing);
        PlayerInputsHandler.OnClientReady();
    }

    public override void OnClientTransportException(Exception exception)
    {
        if (ClientState != NetworkClientState.AuthFailed)
            SetClientState(NetworkClientState.Error, exception.Message);
    }

    public override void OnClientDisconnect()
    {
        if (ClientState == NetworkClientState.Connecting)
            SetClientState(NetworkClientState.ConnectionFailed);
        base.OnClientDisconnect();
    }

    public override void OnClientError(TransportError error, string reason)
    {
        if (ClientState == NetworkClientState.AuthFailed)
            return;
        if (error is TransportError.Timeout or TransportError.DnsResolve or TransportError.Congestion)
            SetClientState(NetworkClientState.ConnectionFailed, reason);
        else if (error is TransportError.Refused or TransportError.ConnectionClosed)
            SetClientState(NetworkClientState.Disconnected, reason);
        else
            SetClientState(NetworkClientState.Error, reason);
    }

    public override void Update()
    {
        base.Update();
        if (!NetworkClient.active && !IsErrorState && ClientState is not NetworkClientState.None)
            SetClientState(NetworkClientState.None);
        if (NetworkClient.active)
            PlayerInputsHandler.OnClientUpdate();
    }

    public override void OnStopClient()
    {
        if (authenticator)
            authenticator.OnClientAuthenticated.RemoveListener(OnClientAuthenticated);
        if (!IsErrorState)
            SetClientState(NetworkClientState.None);
        PlayerInputsHandler.OnStopClient();
        ClientLastTeamChoiceKickTime = 0;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnServerAuthenticated(NetworkConnectionToClient conn)
    {
        ServerOnPlayerJoin?.Invoke(conn);
    }

    private void OnClientAuthenticated()
    {
        if (CurrentSceneName == OnlineSceneName)
            SetClientState(NetworkClientState.Playing);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == OnlineSceneName && NetworkClient.active && NetworkClient.connection.isAuthenticated)
            SetClientState(NetworkClientState.Playing);
    }

    public void SetClientState(NetworkClientState state, string errorMessage = "")
    {
        if (state is NetworkClientState.None)
            NetworkClient.Shutdown();
        ClientState = state;
        ClientStateErrorMessage = errorMessage;
        OnClientStateSet?.Invoke(ClientState);
    }

    private void ConfigureClient(string address, ushort port)
    {
        if (transport is PortTransport portTransport)
            portTransport.Port = port;
        networkAddress = address;
    }

    public void ApplyServerConfig()
    {
        if (!serverConfig)
            return;

        var configParams = serverConfig.GetConfigParams();

        if (authenticator is GameNetworkAuthenticator gameNetworkAuthenticator)
            gameNetworkAuthenticator.serverPassword = configParams.serverPassword;

        if (transport is PortTransport portTransport)
        {
            portTransport.Port = configParams.port;
        }

        maxConnections = configParams.maxPlayers;
        disconnectInactiveConnections = configParams.kickAfk;
        disconnectInactiveTimeout = configParams.kickAfkTimeout;
    }

    public void ApplyClientConfig()
    {
        if (!clientConfig)
            return;
        var configParams = clientConfig.GetConfigParams();
        ConfigureClient(configParams.address, configParams.port);
    }

    public void StartServerWithConfig()
    {
        ApplyServerConfig();
        StartServer();
    }

    public void StartHostWithConfig()
    {
        if (!IsErrorState)
            SetClientState(NetworkClientState.Connecting);
        ApplyServerConfig();
        if (transport is SimpleWebTransport webTransport)
            webTransport.clientUseWss = webTransport.sslEnabled;
        StartHost();
    }

    public void StartClientWithConfig()
    {
        if (!IsErrorState)
            SetClientState(NetworkClientState.Connecting);
        ApplyClientConfig();
        StartClient();
    }

    public void StartClientOfficialServer(string address)
    {
        string host = address;
        ushort port = 443;

        var parts = address.Split(':');
        if (parts.Length == 2 && ushort.TryParse(parts[1], out ushort parsedPort))
        {
            host = parts[0];
            port = parsedPort;
        }

        ConfigureClient(host, port);
        StartClient();
    }

    public void ResetClientErrorState()
    {
        if (IsErrorState)
            SetClientState(NetworkClientState.None);
    }
}