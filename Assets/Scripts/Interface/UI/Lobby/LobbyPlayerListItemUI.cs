using UnityEngine;
using UnityEngine.UI;
public class LobbyPlayerListItemUI : MonoBehaviour
{
    [Header("UI")]
    public Text infoText;

    private int _playerId;
    private LobbyMenuUI _menuUI;

    public void SetData(LobbyMenuUI menuUI, NetworkArenaLobbyManager.ArenaLobby.Player player, Color color)
    {
        _menuUI = menuUI;
        _playerId = player.connId;
        var playerName = NetworkPlayerList.Singleton && NetworkPlayerList.Singleton.Players.TryGetValue(_playerId, out var plr) ? plr.name : "";
        playerName = playerName.Trim();
        if (playerName.Length == 0)
            playerName = "p" + _playerId.ToString();
        infoText.text = $"{playerName} (group {player.groupId})";
        infoText.color = new Color(color.r, color.g, color.b, infoText.color.a);
    }

    public void SwitchPlayerTeam()
    {
        _menuUI.SwitchPlayerTeam(_playerId);
    }

    public void KickPlayer()
    {
        _menuUI.KickPlayer(_playerId);
    }
}
