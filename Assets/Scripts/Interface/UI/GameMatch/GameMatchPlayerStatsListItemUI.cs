using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class GameMatchPlayerStatsListItemUI : MonoBehaviour
{
    [Header("UI")]
    public Image tankIconImage;
    public Text playerNameText;
    public Text killsText;
    public Text assistsText;
    public Text deathsText;
    public Text damageText;

    public void SetData(NetworkArenaPlayerStats.PlayerStats stats)
    {
        tankIconImage.sprite = NetworkShop.Singleton
            ? NetworkShop.Singleton.tankOffers.FirstOrDefault(offer => offer.tankId == stats.tankId).icon
            : null;
        var color = GameTanksManager.Singleton.GetTeam(stats.teamId).color;
        tankIconImage.color = new(color.r, color.g, color.b, tankIconImage.color.a);
        playerNameText.text = stats.playerName;
        playerNameText.color = new(color.r, color.g, color.b, tankIconImage.color.a);
        killsText.text = stats.kills.ToString();
        assistsText.text = stats.assists.ToString();
        deathsText.text = stats.deaths.ToString();
        damageText.text = stats.damage.ToString() + (stats.heal > 0 ? $"/{stats.heal}" : "");
    }
}
