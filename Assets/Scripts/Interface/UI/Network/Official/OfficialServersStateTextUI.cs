using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class OfficialServersStateTextUI : MonoBehaviour
{

    [SerializeField]
    private OfficialServersLoader serversLoader;

    [Header("UI")]
    [SerializeField]
    private Text[] stateTexts;

    [Header("Localized Strings")]
    [SerializeField]
    private LocalizedString currentServerText;
    [SerializeField]
    private LocalizedString noSelectedServerText;
    [SerializeField]
    private LocalizedString loadingText;
    [SerializeField]
    private LocalizedString errorText;

    private string lastServerId = null;
    private float timeUntilTextUpdate;
    private int textDotsCount;
    private OfficialServersLoader.State lastState;

    private void Update()
    {
        if (lastServerId != serversLoader.CurrentServerId)
        {
            lastServerId = serversLoader.CurrentServerId;
            ShowState(serversLoader.CurrentState);
        }
        timeUntilTextUpdate -= Time.deltaTime;
        if (timeUntilTextUpdate > 0)
            return;
        timeUntilTextUpdate = 0.3f;
        if (lastState != serversLoader.CurrentState || textDotsCount >= 3)
        {
            textDotsCount = 0;
            lastState = serversLoader.CurrentState;
            ShowState(lastState);
            return;
        }
        if (lastState == OfficialServersLoader.State.Success)
            return;
        foreach(var stateText in stateTexts)
            stateText.text += ".";
        textDotsCount = textDotsCount < 3 ? textDotsCount + 1 : 0;
    }

    private void ShowState(OfficialServersLoader.State state)
    {
        StopAllCoroutines();
        switch (state)
        {
            case OfficialServersLoader.State.Loading:
                loadingText.LoadWithCallback(result => {
                    foreach (var stateText in stateTexts)
                        stateText.text = result;
                });
                break;
            case OfficialServersLoader.State.Success:
                try
                {
                    var currentServer = serversLoader.Servers.First(server => server.id == serversLoader.CurrentServerId);
                    currentServerText.LoadWithCallback(result => {
                        foreach (var stateText in stateTexts)
                            stateText.text = result + ": " + currentServer.name;
                    });
                }
                catch
                {
                    noSelectedServerText.LoadWithCallback(result => {
                        foreach (var stateText in stateTexts)
                            stateText.text = result;
                    });
                }
                break;
            case OfficialServersLoader.State.Failure:
                errorText.LoadWithCallback(result => {
                    foreach (var stateText in stateTexts) stateText.text = result;
                });
                break;
        }
    }

}