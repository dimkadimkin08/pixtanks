using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LobbyPlayerListUI : MonoBehaviour
{
    [Header("References")]
    public LobbyMenuUI lobbyMenuUI;
    public RectTransform contentRoot;
    public LobbyPlayerListItemUI itemPrefab;

    [Range(0, 1)]
    public float itemHeightPercent = 0.2f;

    private NetworkArenaLobbyManager LobbyManager => NetworkArenaLobbyManager.Singleton;

    private readonly Dictionary<int, LobbyPlayerListItemUI> items = new();

    private string currentLobbyId = "";


    private void OnEnable()
    {
        if (LobbyManager)
        {
            LobbyManager.lobbies.OnChange += OnLobbiesChanged;
            RebuildAll();
        }
    }

    private void OnDisable()
    {
        if (LobbyManager)
            LobbyManager.lobbies.OnChange -= OnLobbiesChanged;
    }

    private void Update()
    {
        if (lobbyMenuUI.SelectedLobbyId != currentLobbyId)
        {
            currentLobbyId = lobbyMenuUI.SelectedLobbyId;
            RebuildAll();
        }
    }

    private void OnLobbiesChanged(
        SyncIDictionary<string, NetworkArenaLobbyManager.ArenaLobby>.Operation operation,
        string key,
        NetworkArenaLobbyManager.ArenaLobby player)
    {
        if (lobbyMenuUI.SelectedLobbyId != "" && key == lobbyMenuUI.SelectedLobbyId)
        {
            RebuildAll();
        }
    }

    private void RebuildAll()
    {
        ClearAll();

        if (string.IsNullOrEmpty(currentLobbyId))
        {
            return;
        }

        if (!LobbyManager.lobbies.ContainsKey(currentLobbyId))
        {
            lobbyMenuUI.SelectLobby("");
            return;
        }

        var lobby = LobbyManager.lobbies[currentLobbyId];

        var players = lobby.players.OrderBy(plr => plr.connId);

        var groupColors = new List<Color>();
        foreach (var group in lobby.groups)
        {
            groupColors.Add(GameTanksManager.Singleton.GetTeam(group.teamId).color);
        }

        foreach (var player in players)
            UpdateOrCreateItem(player.connId, player, groupColors[player.groupId]);
    }

    private void UpdateOrCreateItem(int playerId, NetworkArenaLobbyManager.ArenaLobby.Player player, Color color)
    {
        if (!items.TryGetValue(playerId, out var item))
        {
            item = Instantiate(itemPrefab, contentRoot);
            items[playerId] = item;
            ResizeContentRoot();
        }

        item.SetData(lobbyMenuUI, player, color);
    }

    private void ClearAll()
    {
        foreach (var item in items.Values)
            Destroy(item.gameObject);

        items.Clear();
        ResizeContentRoot();
    }

    private void ResizeContentRoot()
    {
        contentRoot.anchorMax = new Vector2(1, 1);
        contentRoot.anchorMin = new Vector2(0, 1 - items.Count * itemHeightPercent);
    }

}