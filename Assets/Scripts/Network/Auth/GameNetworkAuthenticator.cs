using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mirror;
using UnityEngine;
public class GameNetworkAuthenticator : NetworkAuthenticator
{
    public static int LocalPlayerConnId;

    private const float DisconnectDelay = 2;

    public static string ClientPassword = "";

    public static AuthResponse LastResponse;

    public enum AuthResponse { Success, ErrorDifferentVersion, ErrorBadUsername, ErrorUsernameTaken, ErrorWrongPassword, ErrorOther }

    public string serverPassword;

    [SerializeField]
    private int minNameLength = 1;
    [SerializeField]
    private int maxNameLength = 20;

    private readonly HashSet<NetworkConnectionToClient> _connectionsPendingDisconnect = new();

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
    }

    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<AuthRequestMessage>();
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
    }

    public override void OnStopClient()
    {
        NetworkClient.UnregisterHandler<AuthResponseMessage>();
    }

    public void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
    {
        if (conn.isAuthenticated || _connectionsPendingDisconnect.Contains(conn))
            return;

        var response = AuthResponse.Success;
        var username = "";

        if (Application.version != msg.ClientGameVersion)
            response = AuthResponse.ErrorDifferentVersion;
        else if (serverPassword != msg.AuthPassword)
            response = AuthResponse.ErrorWrongPassword;
        else if (!FormatUsername(msg.AuthUsername, out username))
            response = AuthResponse.ErrorBadUsername;
        else if (NetworkServer.connections.Values.Any(otherConn => otherConn.Username() == username))
            response = AuthResponse.ErrorUsernameTaken;

        conn.Send(new AuthResponseMessage { connId = conn.connectionId, Response = response });

        if (response == AuthResponse.Success)
        {
            Debug.Log($"Player join: name: {username}; address: {conn.address}; connId: {conn.connectionId}; additionalInfo: {msg.ClientAdditionalInfo}; {(msg.AuthPassword.Length > 0 ? $"password: {msg.AuthPassword}" : "no password")}");
            conn.authenticationData = username;
            ServerAccept(conn);
        }
        else
        {
            Debug.Log($"Player reject: reason: {response}; name: {username}; address: {conn.address}; connId: {conn.connectionId}; additionalInfo: {msg.ClientAdditionalInfo}; {(msg.AuthPassword != null && msg.AuthPassword.Length > 0 ? $"password: {msg.AuthPassword}" : "no password")}");
            _connectionsPendingDisconnect.Add(conn);
            StartCoroutine(DelayedDisconnect(conn));
        }
    }

    public override void OnClientAuthenticate()
    {
        var playerName = GameSettings.CurrentPlayerName.Trim();
        NetworkClient.Send(new AuthRequestMessage
        {
            AuthUsername = playerName.Length > 0 ? playerName : "Player",
            AuthPassword = ClientPassword,
            ClientGameVersion = Application.version,
            ClientAdditionalInfo = AppPlatform.Platform.ToString()
        });
    }

    private void OnAuthResponseMessage(AuthResponseMessage msg)
    {
        LocalPlayerConnId =  -1;
        LastResponse = msg.Response;
        if (msg.Response == AuthResponse.Success)
        {
            ClientAccept();
            LocalPlayerConnId = msg.connId;
        }
        else
        {
            GameNetworkManager.Singleton.SetClientState(GameNetworkManager.NetworkClientState.AuthFailed);
            ClientReject();
        }
    }

    private IEnumerator DelayedDisconnect(NetworkConnectionToClient conn)
    {
        yield return new WaitForSeconds(DisconnectDelay);

        ServerReject(conn);

        yield return null;

        _connectionsPendingDisconnect.Remove(conn);
    }

    private bool FormatUsername(string input, out string result)
    {
        //if (Regex.IsMatch(input, "^p-?[0-9]+$"))
        //{
        //    result = "";
        //    return false;
        //}
        //var sb = new StringBuilder();
        //foreach (var c in input.Trim().Where(c => c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= 'À' and <= 'ß' or >= 'à' and <= 'ÿ' or ' ' or '.' or '_'))
        //    if (sb.Length < maxNameLength)
        //        sb.Append(c);
        //    else
        //        break;
        //result = sb.ToString();
        result = input.Trim()[..Mathf.Min(input.Trim().Length, maxNameLength)].Replace("\"", "");
        if (result.Length < minNameLength)
            return false;
        var names = NetworkServer.connections.Values.Select(conn => conn.Username()).ToHashSet();

        string uniqueResult = result;
        int counter = 1;

        while (names.Contains(uniqueResult))
        {
            Debug.Log("Trying name " + counter);
            uniqueResult = result + counter;
            counter++;
            if (counter > 99)
                return true;
        }

        result = uniqueResult;
        return true;
    }

    public struct AuthRequestMessage : NetworkMessage
    {
        public string AuthUsername;
        public string AuthPassword;
        public string ClientGameVersion;
        public string ClientAdditionalInfo;
    }

    public struct AuthResponseMessage : NetworkMessage
    {
        public int connId;
        public AuthResponse Response;
    }
}