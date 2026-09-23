using Mirror;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class GameMatchKillListItemUI : MonoBehaviour
{
    [Header("Killer")]
    public Image killerIcon;
    public Text killerName;

    [Header("Assist")]
    public GameObject assistIndicator;
    public Image assistIcon;
    public Text assistName;
    public GameObject moreAssistsIndicator;
    public Text moreAssistsText;

    [Header("Victim")]
    public Image victimIcon;
    public Text victimName;

    [Header("Animation")]
    public AnimationCurve moveCurve;
    public float lifeTime = 5f;
    public float moveWidthMultiplier = 1f;

    [Header("Events")]
    public UnityEvent onFirstSpawn;

    RectTransform rect;
    double killTime;

    [HideInInspector]
    public NetworkArenaPlayerStats.KillData killData;

    public void SetData(NetworkArenaPlayerStats.KillData data, NetworkArenaPlayerStats stats, bool isFirstSpawn)
    {
        killData = data;
        killTime = data.timestamp;
        rect = (RectTransform)transform;
        gameObject.SetActive(NetworkTime.time - killTime < lifeTime);

        SetPlayer(data.killedByPlayer, stats, killerIcon, killerName);
        SetPlayer(data.killedPlayer, stats, victimIcon, victimName);
        SetAssist(data, stats);
        UpdatePos();

        if (isFirstSpawn)
        {
            onFirstSpawn?.Invoke();
        }
    }

    private void Update()
    {
        UpdatePos();
    }

    private void UpdatePos()
    {
        float t = (float)(NetworkTime.time - killTime);

        if (t >= lifeTime)
        {
            gameObject.SetActive(false);
            return;
        }

        float k = moveCurve.Evaluate(t / lifeTime);
        float offsetX = rect.rect.width * moveWidthMultiplier;

        rect.pivot = new Vector2(0f, rect.pivot.y);
        rect.anchoredPosition = new Vector2(k * offsetX, rect.anchoredPosition.y);
    }

    private void SetAssist(NetworkArenaPlayerStats.KillData data, NetworkArenaPlayerStats stats)
    {
        bool hasAssist = data.assistPlayers?.Length > 0;

        assistIcon.gameObject.SetActive(hasAssist);
        assistName.gameObject.SetActive(hasAssist);
        assistIndicator.SetActive(hasAssist);
        moreAssistsIndicator.SetActive(false);

        if (!hasAssist) return;

        SetPlayer(data.assistPlayers[0], stats, assistIcon, assistName);

        int extra = data.assistPlayers.Length - 1;
        if (extra > 0)
        {
            moreAssistsIndicator.SetActive(true);
            moreAssistsText.text = $"{extra}";
        }
    }

    private void SetPlayer(int playerId, NetworkArenaPlayerStats stats, Image icon, Text nameText)
    {
        if (!stats.playerStats.TryGetValue(playerId, out var ps))
            return;

        icon.sprite = NetworkShop.Singleton
            ? NetworkShop.Singleton.tankOffers
                .FirstOrDefault(o => o.tankId == ps.tankId).icon
            : null;

        var c = GameTanksManager.Singleton.GetTeam(ps.teamId).color;
        icon.color = new Color(c.r, c.g, c.b, icon.color.a);

        nameText.text = NetworkPlayerList.Singleton
            ? NetworkPlayerList.Singleton.Players[playerId].name
            : string.Empty;
    }
}
