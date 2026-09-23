using UnityEngine;
public class MainMenuNavigation : MonoBehaviour
{
    [SerializeField]
    private GameObject welcomeMenu;
    [SerializeField]
    private GameObject startMenu;
    [SerializeField]
    private GameObject serversMenu;
    [SerializeField]
    private GameObject hostMenu;
    [SerializeField]
    private GameObject clientMenu;
    [SerializeField]
    private GameObject settingsMenu;

    private void Start()
    {
        if (GameSettings.CurrentPlayerName == "")
            OpenWelcomeMenu();
        else
            OpenStartMenu();
    }

    public void OpenWelcomeMenu()
    {
        welcomeMenu.SetActive(true);
        startMenu.SetActive(false);
        serversMenu.SetActive(false);
        hostMenu.SetActive(false);
        clientMenu.SetActive(false);
        settingsMenu.SetActive(false);
    }

    public void CloseWelcomeMenuIfPossible()
    {
        if (GameSettings.CurrentPlayerName != "")
                OpenStartMenu();
    }

    public void OpenStartMenu()
    {
        welcomeMenu.SetActive(false);
        startMenu.SetActive(true);
        serversMenu.SetActive(false);
        hostMenu.SetActive(false);
        clientMenu.SetActive(false);
        settingsMenu.SetActive(false);
    }

    public void OpenServersMenu()
    {
        welcomeMenu.SetActive(false);
        startMenu.SetActive(false);
        serversMenu.SetActive(true);
        hostMenu.SetActive(false);
        clientMenu.SetActive(false);
        settingsMenu.SetActive(false);
    }

    public void OpenHostMenu()
    {
        welcomeMenu.SetActive(false);
        startMenu.SetActive(false);
        serversMenu.SetActive(false);
        hostMenu.SetActive(true);
        clientMenu.SetActive(false);
        settingsMenu.SetActive(false);
    }

    public void OpenClientMenu()
    {
        welcomeMenu.SetActive(false);
        startMenu.SetActive(false);
        serversMenu.SetActive(false);
        hostMenu.SetActive(false);
        clientMenu.SetActive(true);
        settingsMenu.SetActive(false);
    }

    public void OpenSettingsMenu()
    {
        welcomeMenu.SetActive(false);
        startMenu.SetActive(false);
        serversMenu.SetActive(false);
        hostMenu.SetActive(false);
        clientMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

}
