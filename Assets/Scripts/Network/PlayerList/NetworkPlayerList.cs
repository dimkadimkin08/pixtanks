using System;
using Mirror;
public class NetworkPlayerList : NetworkBehaviour
{

    public static NetworkPlayerList Singleton;

    public readonly SyncDictionary<int, Player> Players = new();

    public NetworkPlayerList()
    {
        Singleton = this;
    }

    private void Awake()
    {
        Singleton = this;
    }

    public override void OnStartServer()
    {
        GameNetworkManager.Singleton.ServerOnPlayerJoin += ServerOnPlayerJoin;
        GameNetworkManager.Singleton.ServerOnPlayerLeave += ServerOnPlayerLeave;
    }

    public override void OnStopServer()
    {
        GameNetworkManager.Singleton.ServerOnPlayerJoin -= ServerOnPlayerJoin;
        GameNetworkManager.Singleton.ServerOnPlayerLeave -= ServerOnPlayerLeave;
    }

    [Server]
    private void ServerOnPlayerJoin(NetworkConnectionToClient conn)
    {
        Players[conn.connectionId] = new Player { id = conn.connectionId, name = conn.Username() };
    }

    [Server]
    private void ServerOnPlayerLeave(NetworkConnectionToClient conn)
    {
        Players.Remove(conn.connectionId);
    }

    [Serializable]
    public struct Player
    {
        public int id;
        public string name;
    }
}
