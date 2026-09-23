using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyListUI : MonoBehaviour
{
    [Header("References")]
    public LobbyMenuUI lobbyMenuUI;
    public RectTransform contentRoot;
    public LobbyListItemUI itemPrefab;
    [Range(0, 1)]
    public float itemHeightPercent = 0.2f;

    private NetworkArenaLobbyManager LobbyManager => NetworkArenaLobbyManager.Singleton;

    private readonly Dictionary<string, LobbyListItemUI> items = new();

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

    private void OnLobbiesChanged(SyncIDictionary<string, NetworkArenaLobbyManager.ArenaLobby>.Operation operation, string key, NetworkArenaLobbyManager.ArenaLobby _)
    {
        switch (operation)
        {
            case SyncIDictionary<string, NetworkArenaLobbyManager.ArenaLobby>.Operation.OP_ADD:
            case SyncIDictionary<string, NetworkArenaLobbyManager.ArenaLobby>.Operation.OP_SET:
                UpdateOrCreateItem(key, LobbyManager.lobbies[key]);
                break;

            case SyncIDictionary<string, NetworkArenaLobbyManager.ArenaLobby>.Operation.OP_REMOVE:
                RemoveItem(key);
                break;

            case SyncIDictionary<string, NetworkArenaLobbyManager.ArenaLobby>.Operation.OP_CLEAR:
                ClearAll();
                break;
        }
    }

    private void RebuildAll()
    {
        ClearAll();

        foreach (var kvp in LobbyManager.lobbies)
            UpdateOrCreateItem(kvp.Key, kvp.Value);
    }

    private void UpdateOrCreateItem(string lobbyId, NetworkArenaLobbyManager.ArenaLobby stats)
    {
        if (!items.TryGetValue(lobbyId, out var item))
        {
            item = Instantiate(itemPrefab, contentRoot);
            items[lobbyId] = item;
            ResizeContentRoot();
        }

        item.SetData(lobbyMenuUI, stats);
    }

    private void RemoveItem(string lobbyId)
    {
        if (items.TryGetValue(lobbyId, out var item))
        {
            Destroy(item.gameObject);
            items.Remove(lobbyId);
            ResizeContentRoot();
        }
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
        contentRoot.anchorMax = new(1, 1);
        contentRoot.anchorMin = new(0, 1 - items.Count * itemHeightPercent);
    }
}
