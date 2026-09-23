using UnityEngine;
using Mirror;
public class TankSpecial : NetworkBehaviour
{
    private bool _serverIsSpecialPressed = false;

    [Server]
    public bool ServerGetSpecialPressed()
    {
        return _serverIsSpecialPressed;
    }

    [Server]
    public void ServerSetSpecialPressed(bool isSpecialPressed)
    {
        _serverIsSpecialPressed = isSpecialPressed;
    }
}