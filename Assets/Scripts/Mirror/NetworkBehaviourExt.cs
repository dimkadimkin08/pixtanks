using Mirror;
public static class NetworkBehaviourExt
{

    /// <summary>
    /// Destroy gameObject on server and all clients
    /// </summary>
    /// <param name="net">This NetworkBehaviour</param>
    [Server]
    public static void DestroyNetObject(this NetworkBehaviour net)
    {
        var netIdentity = net.netIdentity;
        NetworkServer.Destroy(netIdentity.gameObject);
        //UnityEngine.Object.Destroy(netIdentity.gameObject);
    }
}
