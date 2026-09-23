using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameMatchKillListUI : MonoBehaviour
{
    [Header("References")]
    public NetworkArenaPlayerStats statsSync;
    public RectTransform contentRoot;
    public GameMatchKillListItemUI itemPrefab;

    [Header("Settings")]
    [Min(0)]
    public int maxItems = 5;

    [Header("Events")]
    public UnityEvent onAllyKiller;
    public UnityEvent onAllyDied;
    public UnityEvent onOtherKill;

    private readonly List<GameMatchKillListItemUI> items = new();

    private void OnEnable()
    {
        if (statsSync != null)
        {
            statsSync.kills.OnChange += OnStatsChanged;
            RebuildAll();
        }
    }

    private void OnDisable()
    {
        if (statsSync != null)
            statsSync.kills.OnChange -= OnStatsChanged;
    }

    private void OnStatsChanged(
        SyncList<NetworkArenaPlayerStats.KillData>.Operation operation,
        int index,
        NetworkArenaPlayerStats.KillData _)
    {
        RebuildAll();
    }

    private void RebuildAll()
    {
        var oldKills = items.Select(item => item.killData.index);
        ClearItems();

        if (statsSync == null)
            return;

        List<NetworkArenaPlayerStats.KillData> sorted =
            statsSync.kills
                .OrderBy(k => k.timestamp)
                .ToList();

        if (maxItems > 0 && sorted.Count > maxItems)
            sorted = sorted.TakeLast(maxItems).ToList();

        foreach (var data in sorted)
        {
            GameMatchKillListItemUI item = Instantiate(itemPrefab, contentRoot);
            var isFirstSpawn = !oldKills.Contains(data.index) && NetworkTime.time - data.timestamp < item.lifeTime;
            item.SetData(data, statsSync, isFirstSpawn);
            items.Add(item);

            if (isFirstSpawn)
            {
                var localPlayerTeam = NetworkTank.LocalPlayer ? NetworkTank.LocalPlayer.Parameters.TeamId : "";
                if (localPlayerTeam != "")
                {
                    if (statsSync.playerStats.TryGetValue(data.killedByPlayer, out var killer) && localPlayerTeam == killer.teamId)
                        onAllyKiller?.Invoke();
                    else if (statsSync.playerStats.TryGetValue(data.killedPlayer, out var victim) && localPlayerTeam == victim.teamId)
                        onAllyDied?.Invoke();
                    else
                        onOtherKill?.Invoke();
                }
            }

            float itemHeight = contentRoot.rect.height / maxItems;

            if (!item.TryGetComponent<LayoutElement>(out var layout))
                layout = item.gameObject.AddComponent<LayoutElement>();

            layout.preferredHeight = itemHeight;
            layout.minHeight = itemHeight;
            layout.flexibleHeight = 0;
        }
    }

    private void ClearItems()
    {
        foreach (var item in items)
            Destroy(item.gameObject);

        items.Clear();
    }
}