using System;
using UnityEngine;
using UnityEngine.UI;
using static GameTanksManager;
public class CurrentTankSpriteUI : MonoBehaviour
{
    [SerializeField]
    private Image tankImage;

    private void OnEnable()
    {
        NetworkTank.OnLocalPlayerSpawn += OnLocalPlayerSpawn;
        NetworkTank.OnLocalPlayerDead += OnLocalPlayerDead;
        NetworkShop.OnLocalPlayerSelectedTankChanged += OnPlayerSelectedTankChanged;
        Refresh();
    }

    private void OnDisable()
    {
        NetworkTank.OnLocalPlayerSpawn -= OnLocalPlayerSpawn;
        NetworkTank.OnLocalPlayerDead -= OnLocalPlayerDead;
        NetworkShop.OnLocalPlayerSelectedTankChanged -= OnPlayerSelectedTankChanged;
    }

    private void OnLocalPlayerDead()
    {
        Refresh();
    }

    private void OnLocalPlayerSpawn(NetworkTank tank)
    {
        Refresh();
        NetworkTank.LocalPlayer.Parameters.OnTeamIdSet += OnTeamChanged;
    }

    private void OnTeamChanged(string old, string current)
    {
        ShowTeamColor();
    }

    private void ShowTeamColor()
    {
        if (tankImage && NetworkTank.LocalPlayer)
        {
            var color = Color.white;
            if (NetworkTank.LocalPlayer)
                color = Singleton.GetTeam(NetworkTank.LocalPlayer.Parameters.TeamId).color;
            tankImage.color = new Color(color.r, color.g, color.b, tankImage.color.a);
        }
    }

    private void OnPlayerSelectedTankChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (tankImage)
        {
            Sprite newTankSprite = null;
            if (NetworkShop.Singleton)
            {
                foreach (var offer in NetworkShop.Singleton.tankOffers)
                {
                    if (offer.tankId == NetworkShop.CurrentTankId)
                    {
                        newTankSprite = offer.icon;
                        break;
                    }
                }
            }
            tankImage.sprite = newTankSprite;
            if (NetworkTank.LocalPlayer)
                ShowTeamColor();
        }
    }
}