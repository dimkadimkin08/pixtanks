using Mirror;
using UnityEngine;
using UnityEngine.UI;
using static GameTanksManager;

public class NetworkMatchScoreUI : NetworkBehaviour
{
    [SerializeField, SyncVar]
    private int firstTeamScore = 0;
    [SerializeField, SyncVar]
    private int secondTeamScore = 0;
    [SerializeField, SyncVar]
    private int thirdTeamScore = 0;

    [SerializeField, SyncVar(hook = nameof(HookFirstTeam))]
    private string firstTeam = "";
    [SerializeField, SyncVar(hook = nameof(HookSecondTeam))]
    private string secondTeam = "";
    [SerializeField, SyncVar(hook = nameof(HookThirdTeam))]
    private string thirdTeam = "";

    [Space]

    [SerializeField]
    private Image[] firstTeamColoredImages;
    [SerializeField]
    private Image[] secondTeamColoredImages;
    [SerializeField]
    private Image[] thirdTeamColoredImages;

    [SerializeField]
    private Text[] firstTeamScoreText;
    [SerializeField]
    private Text[] secondTeamScoreText;
    [SerializeField]
    private Text[] thirdTeamScoreText;

    private void OnEnable()
    {
        if (isClient)
        {
            ShowFirstTeamColor(firstTeam);
            ShowSecondTeamColor(secondTeam);
            ShowThirdTeamColor(thirdTeam);
        }
    }

    public override void OnStartClient()
    {
        ShowFirstTeamColor(firstTeam);
        ShowSecondTeamColor(secondTeam);
        ShowThirdTeamColor(thirdTeam);
    }

    private void Update()
    {
        if (isClient)
        {
            if (firstTeam != "")
                foreach (var text in firstTeamScoreText)
                    if (text)
                        text.text = firstTeamScore.ToString();

            if (secondTeam != "")
                foreach (var text in secondTeamScoreText)
                    if (text)
                        text.text = secondTeamScore.ToString();

            if (thirdTeam != "")
                foreach (var text in thirdTeamScoreText)
                    if (text)
                        text.text = thirdTeamScore.ToString();
        }
    }

    private void HookFirstTeam(string old, string current)
    {
        ShowFirstTeamColor(current);
    }

    private void HookSecondTeam(string old, string current)
    {
        ShowSecondTeamColor(current);
    }

    private void HookThirdTeam(string old, string current)
    {
        ShowThirdTeamColor(current);
    }

    private void ShowFirstTeamColor(string teamId)
    {
        var team = GameTanksManager.Singleton.GetTeam(teamId);
        var teamFound = teamId != "" && team != default;

        foreach (var image in firstTeamColoredImages)
            if (image)
            {
                image.gameObject.SetActive(teamFound);
                if (teamFound)
                    image.color = new Color(team.color.r, team.color.g, team.color.b, image.color.a);
            }

        foreach (var text in firstTeamScoreText)
            if (text)
            {
                text.gameObject.SetActive(teamFound);
                if (teamFound)
                    text.color = new Color(team.color.r, team.color.g, team.color.b, text.color.a);
            }
    }

    private void ShowSecondTeamColor(string teamId)
    {
        var team = GameTanksManager.Singleton.GetTeam(teamId);
        var teamFound = teamId != "" && team != default;

        foreach (var image in secondTeamColoredImages)
            if (image)
            {
                image.gameObject.SetActive(teamFound);
                if (teamFound)
                    image.color = new Color(team.color.r, team.color.g, team.color.b, image.color.a);
            }

        foreach (var text in secondTeamScoreText)
            if (text)
            {
                text.gameObject.SetActive(teamFound);
                if (teamFound)
                    text.color = new Color(team.color.r, team.color.g, team.color.b, text.color.a);
            }
    }

    private void ShowThirdTeamColor(string teamId)
    {
        var team = GameTanksManager.Singleton.GetTeam(teamId);
        var teamFound = teamId != "" && team != default;

        foreach (var image in thirdTeamColoredImages)
            if (image)
            {
                image.gameObject.SetActive(teamFound);
                if (teamFound)
                    image.color = new Color(team.color.r, team.color.g, team.color.b, image.color.a);
            }

        foreach (var text in thirdTeamScoreText)
            if (text)
            {
                text.gameObject.SetActive(teamFound);
                if (teamFound)
                    text.color = new Color(team.color.r, team.color.g, team.color.b, text.color.a);
            }
    }

    [Server]
    public void ServerSetFirstTeamScore(int score)
    {
        firstTeamScore = score;
    }

    [Server]
    public void ServerSetSecondTeamScore(int score)
    {
        secondTeamScore = score;
    }

    [Server]
    public void ServerSetThirdTeamScore(int score)
    {
        thirdTeamScore = score;
    }

    [Server]
    public void ServerSetFirstTeam(string team)
    {
        firstTeam = team;
        if (isClient)
            ShowFirstTeamColor(team);
    }

    [Server]
    public void ServerSetSecondTeam(string team)
    {
        secondTeam = team;
        if (isClient)
            ShowSecondTeamColor(team);
    }

    [Server]
    public void ServerSetThirdTeam(string team)
    {
        thirdTeam = team;
        if (isClient)
            ShowThirdTeamColor(team);
    }
}