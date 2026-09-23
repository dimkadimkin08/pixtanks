using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class OfficialServerPlayButton : MonoBehaviour
{
    [SerializeField]
    private Button playButton;
    [SerializeField]
    private OfficialServersLoader serversLoader;

    private void Start()
    {
        playButton.onClick.AddListener(() =>
        {
            if (GameSettings.CurrentPlayerName.Trim().Length > 0)
                serversLoader.JoinSelectedServer();
        });
        UpdateState();
    }

    private void OnEnable()
    {
        serversLoader.OnStateChanged += OnStateChanged;
        serversLoader.OnCurrentServerChanged += OnCurrentServerChanged;
        UpdateState();
    }

    private void OnDisable()
    {
        serversLoader.OnStateChanged -= OnStateChanged;
        serversLoader.OnCurrentServerChanged += OnCurrentServerChanged;
    }

    private void OnStateChanged(OfficialServersLoader.State state)
    {
        UpdateState();
    }

    private void OnCurrentServerChanged(string currentServerId)
    {
        UpdateState();
    }

    private void UpdateState()
    {
        playButton.interactable = serversLoader.CurrentState == OfficialServersLoader.State.Success
            && serversLoader.Servers.Any(server => server.id == serversLoader.CurrentServerId);
    }
}