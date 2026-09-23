using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameMatchPlayerStatsListUI : MonoBehaviour
{
    [Header("References")]
    public NetworkArenaPlayerStats statsSync;
    public GameObject container;
    public RectTransform contentRoot;
    public GameMatchPlayerStatsListItemUI itemPrefab;
    [Range(0, 1)]
    public float itemHeightPercent = 0.2f;

    private readonly Dictionary<int, GameMatchPlayerStatsListItemUI> items = new();
    private bool _wasCollapsed;

    private void OnEnable()
    {
        if (statsSync != null)
        {
            statsSync.playerStats.OnChange += OnStatsChanged;
            RebuildAll();
        }
        container.SetActive(Keyboard.current.tabKey.isPressed);
        _wasCollapsed = !Keyboard.current.tabKey.isPressed;
    }

    private void OnDisable()
    {
        if (statsSync != null)
            statsSync.playerStats.OnChange -= OnStatsChanged;
    }

    private void Update()
    {
        if (_wasCollapsed != !Keyboard.current.tabKey.isPressed)
        {
            container.SetActive(Keyboard.current.tabKey.isPressed);
            _wasCollapsed = !Keyboard.current.tabKey.isPressed;
        }
    }

    private void OnStatsChanged(SyncIDictionary<int, NetworkArenaPlayerStats.PlayerStats>.Operation operation, int key, NetworkArenaPlayerStats.PlayerStats _)
    {
        switch (operation)
        {
            case SyncIDictionary<int, NetworkArenaPlayerStats.PlayerStats>.Operation.OP_ADD:
            case SyncIDictionary<int, NetworkArenaPlayerStats.PlayerStats>.Operation.OP_SET:
                UpdateOrCreateItem(key, statsSync.playerStats[key]);
                break;

            case SyncIDictionary<int, NetworkArenaPlayerStats.PlayerStats>.Operation.OP_REMOVE:
                RemoveItem(key);
                break;

            case SyncIDictionary<int, NetworkArenaPlayerStats.PlayerStats>.Operation.OP_CLEAR:
                ClearAll();
                break;
        }
    }

    private void RebuildAll()
    {
        ClearAll();

        foreach (var kvp in statsSync.playerStats)
            UpdateOrCreateItem(kvp.Key, kvp.Value);
    }

    private void UpdateOrCreateItem(int playerId, NetworkArenaPlayerStats.PlayerStats stats)
    {
        if (!items.TryGetValue(playerId, out var item))
        {
            item = Instantiate(itemPrefab, contentRoot);
            items[playerId] = item;
            ResizeContentRoot();
        }

        item.SetData(stats);
    }

    private void RemoveItem(int playerId)
    {
        if (items.TryGetValue(playerId, out var item))
        {
            Destroy(item.gameObject);
            items.Remove(playerId);
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
