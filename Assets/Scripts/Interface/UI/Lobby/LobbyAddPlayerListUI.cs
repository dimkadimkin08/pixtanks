using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LobbyAddPlayerListUI : MonoBehaviour
{
    [Header("References")]
    public LobbyMenuUI lobbyMenuUI;
    public RectTransform contentRoot;
    public Button itemPrefab;

    [Range(0, 1)]
    public float itemHeightPercent = 0.1f;

    private NetworkPlayerList PlayerList => NetworkPlayerList.Singleton;
    private NetworkArenaLobbyManager LobbyManager => NetworkArenaLobbyManager.Singleton;

    private readonly List<PlayerButtonItem> items = new();

    private string currentLobbyId = "";


    private void OnEnable()
    {
        if (PlayerList)
            PlayerList.Players.OnChange += OnPlayerChanged;
        if (LobbyManager)
            LobbyManager.lobbies.OnChange += OnLobbiesChanged;
        RebuildAll();
    }
    private void OnDisable()
    {
        if (PlayerList)
            PlayerList.Players.OnChange -= OnPlayerChanged;
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

    private void OnPlayerChanged(SyncIDictionary<int, NetworkPlayerList.Player>.Operation operation, int arg2, NetworkPlayerList.Player player)
    {
        if (lobbyMenuUI.SelectedLobbyId != "")
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

        if (!LobbyManager || !LobbyManager.lobbies.ContainsKey(currentLobbyId))
        {
            lobbyMenuUI.SelectLobby("");
            return;
        }

        var takenPlayers = LobbyManager.lobbies[currentLobbyId].players;
        var players = PlayerList.Players.Where(player => !takenPlayers.Any(takenPlayer => takenPlayer.connId == player.Key));

        foreach (var player in players)
            UpdateOrCreateItem(player.Value.name);
    }

    private void UpdateOrCreateItem(string player)
    {
        var item = items.FirstOrDefault(item => item.text && item.text.text == player);
        if (item == default)
        {
            var obj = Instantiate(itemPrefab, contentRoot);
            obj.onClick.AddListener(() => lobbyMenuUI.AddPlayer(player));
            item = new()
            {
                button = obj,
                text = obj.transform.GetComponentInChildren<Text>()
            };
            items.Add(item);
            ResizeContentRoot();
        }

        if (item.text)
            item.text.text = player;
    }

    private void ClearAll()
    {
        foreach (var item in items)
        {
            if (!item.button)
                continue;
            item.button.onClick.RemoveAllListeners();
            Destroy(item.button.gameObject);
        }

        items.Clear();
        ResizeContentRoot();
    }

    private void ResizeContentRoot()
    {
        contentRoot.anchorMax = new Vector2(1, 1);
        contentRoot.anchorMin = new Vector2(0, 1 - items.Count * itemHeightPercent);
    }

    private class PlayerButtonItem
    {
        public Button button;
        public Text text;
    }
}