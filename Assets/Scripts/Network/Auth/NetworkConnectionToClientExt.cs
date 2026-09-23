using Mirror;
public static class NetworkConnectionToClientExt
{
    public static string Username(this NetworkConnectionToClient conn)
    {
        return conn.isAuthenticated ? (string)conn.authenticationData : "";
    }
}
