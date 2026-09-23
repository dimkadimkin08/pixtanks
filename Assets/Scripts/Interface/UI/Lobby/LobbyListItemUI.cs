using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class LobbyListItemUI : MonoBehaviour
{
    [Header("UI")]
    public Text infoText;

    private string lobbyId;
    private LobbyMenuUI _menuUI;

    public void SetData(LobbyMenuUI menuUI, NetworkArenaLobbyManager.ArenaLobby lobby)
    {
        _menuUI = menuUI;
        infoText.text = $"{lobby.lobbyId} | {lobby.arenaId} | {lobby.players.Count()} players";
        lobbyId = lobby.lobbyId;
    }

    public void ManageLobby()
    {
        _menuUI.SelectLobby(lobbyId);
    }
}
