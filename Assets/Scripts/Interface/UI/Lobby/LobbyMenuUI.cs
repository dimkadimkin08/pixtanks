using Edgegap;
using Mirror;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class LobbyMenuUI : MonoBehaviour
{
    public static bool IsMenuVisible = false;

    [SerializeField] private GameObject hintContainer;
    [SerializeField] private GameObject lobbyListContainer;
    [SerializeField] private GameObject lobbyManagementContainer;
    [SerializeField] private InputField addPlayerNameInput;
    [SerializeField] private Text createLobbyArenaText;

    private string _savedSelectedLobbyId;
    private string _selectedLobbyId;
    public string SelectedLobbyId => _selectedLobbyId;

    private bool _lobbyManagementAllowed = false;
    private bool _firstOpen = true;
    private int _selectedArenaId = 0;

    private void OnEnable()
    {
        NetworkCommandsProcessor.Singleton.allowedConnections.OnChange += OnAdminPlayersChange;
        UpdateLobbyManagementAllowed();
        GoBack();
        GoBack();
    }

    private void LateUpdate()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame && !addPlayerNameInput.isFocused && !ChatInputUI.IsInputVisible)
        {
            if (lobbyListContainer.activeSelf || lobbyManagementContainer.activeSelf)
            {
                if (_selectedLobbyId != "")
                    Hide();
                else
                    GoBack();
            }
            else
                Open();
        }
    }

    private void OnDisable()
    {
        NetworkCommandsProcessor.Singleton.allowedConnections.OnChange -= OnAdminPlayersChange;
        GoBack();
        GoBack();
        IsMenuVisible = false;
    }

    private void OnAdminPlayersChange(SyncList<int>.Operation operation, int arg2, int arg3)
    {
        if (!_lobbyManagementAllowed)
            UpdateLobbyManagementAllowed();
    }

    private void UpdateLobbyManagementAllowed()
    {
        _lobbyManagementAllowed = NetworkCommandsProcessor.Singleton.allowedConnections.Contains(GameNetworkAuthenticator.LocalPlayerConnId);
        hintContainer.SetActive(_lobbyManagementAllowed);
    }

    public void StartLobby()
    {
        if (_selectedLobbyId == "" || !NetworkArenaLobbyManager.Singleton || !NetworkCommandsProcessor.Singleton)
            return;
        NetworkCommandsProcessor.Singleton.ClientSendCommand($"lobby start {_selectedLobbyId}");
    }

    public void StopLobby()
    {
        if (_selectedLobbyId == "" || !NetworkArenaLobbyManager.Singleton || !NetworkCommandsProcessor.Singleton)
            return;
        NetworkCommandsProcessor.Singleton.ClientSendCommand($"lobby stop {_selectedLobbyId}");
    }

    public void DeleteLobby()
    {
        if (_selectedLobbyId == "" || !NetworkArenaLobbyManager.Singleton || !NetworkCommandsProcessor.Singleton)
            return;
        NetworkCommandsProcessor.Singleton.ClientSendCommand($"lobby delete {_selectedLobbyId}");
    }

    public void SelectLobby(string lobbyId)
    {
        _selectedLobbyId = lobbyId;
        lobbyListContainer.SetActive(_selectedLobbyId == "");
        lobbyManagementContainer.SetActive(_selectedLobbyId != "");
    }

    public void AddPlayer(string playerName)
    {
        if (_selectedLobbyId == "" || !NetworkArenaLobbyManager.Singleton || !NetworkCommandsProcessor.Singleton)
            return;
        var plr = playerName.Trim();
        if (string.IsNullOrEmpty(plr))
            return;
        NetworkCommandsProcessor.Singleton.ClientSendCommand($"lobby join {_selectedLobbyId} \"{plr}\" 0");
        addPlayerNameInput.text = "";
    }

    public void AddPlayerFromNameInput()
    {
        if (_selectedLobbyId == "" || !NetworkArenaLobbyManager.Singleton || !NetworkCommandsProcessor.Singleton)
            return;
        var plr = addPlayerNameInput.text.Trim();
        if (string.IsNullOrEmpty(plr))
            return;
        NetworkCommandsProcessor.Singleton.ClientSendCommand($"lobby join {_selectedLobbyId} \"{plr}\" 0");
        addPlayerNameInput.text = "";
    }

    public void SwitchPlayerTeam(int playerId)
    {
        if (!NetworkArenaLobbyManager.Singleton || !NetworkCommandsProcessor.Singleton)
            return;
        string lobbyId = "";
        NetworkArenaLobbyManager.ArenaLobby lobby = default;
        var oldGroupId = 0;
        var lobbyFound = NetworkArenaLobbyManager.Singleton.lobbies.Any(lobbyKvp =>
        {
            foreach(var player in lobbyKvp.Value.players)
            {
                if (player.connId == playerId)
                {
                    lobbyId = lobbyKvp.Key;
                    lobby = lobbyKvp.Value;
                    oldGroupId = player.groupId;
                    return true;
                }
            }
            return false;
        });
        if (!lobbyFound)
            return;
        var newGroupId = oldGroupId + 1;
        if (newGroupId >= lobby.groups.Length)
            newGroupId = 0;
        NetworkCommandsProcessor.Singleton.ClientSendCommand($"lobby join {lobbyId} p{playerId} {newGroupId}");
    }

    public void KickPlayer(int playerId)
    {
        if (!NetworkArenaLobbyManager.Singleton || !NetworkCommandsProcessor.Singleton)
            return;
        NetworkCommandsProcessor.Singleton.ClientSendCommand($"lobby leave p{playerId}");
    }

    public void Hide()
    {
        _savedSelectedLobbyId = _selectedLobbyId;
        SelectLobby("");
        lobbyListContainer.SetActive(false);
        lobbyManagementContainer.SetActive(false);
        IsMenuVisible = false;
    }

    public void GoBack()
    {
        _savedSelectedLobbyId = "";
        if (_selectedLobbyId != "")
            SelectLobby("");
        else
        {
            lobbyListContainer.SetActive(false);
            lobbyManagementContainer.SetActive(false);
        }
        IsMenuVisible = lobbyListContainer.activeSelf || lobbyManagementContainer.activeSelf;
    }

    public void Open()
    {
        if (!_lobbyManagementAllowed)
            return;
        if (_firstOpen)
        {
            _selectedArenaId = 0;
            if (NetworkArenaLobbyManager.Singleton && NetworkArenaLobbyManager.Singleton.arenas.Count() > 0)
                createLobbyArenaText.text = NetworkArenaLobbyManager.Singleton.arenas[0].id;
        }
        if (_savedSelectedLobbyId != "" && NetworkArenaLobbyManager.Singleton.lobbies.Any(lobby => lobby.Key == _savedSelectedLobbyId))
            SelectLobby(_savedSelectedLobbyId);
        else
        {
            SelectLobby("");
            lobbyListContainer.SetActive(true);
        }
        _savedSelectedLobbyId = "";
        _firstOpen = false;
        IsMenuVisible = true;
    }

    public void SelectNextArena()
    {
        if (!NetworkArenaLobbyManager.Singleton)
            return;
        _selectedArenaId++;
        if (_selectedArenaId >= NetworkArenaLobbyManager.Singleton.arenas.Count)
            _selectedArenaId = 0;
        createLobbyArenaText.text = NetworkArenaLobbyManager.Singleton.arenas[_selectedArenaId].id;
    }

    public void SelectPrevArena()
    {
        if (!NetworkArenaLobbyManager.Singleton)
            return;
        _selectedArenaId--;
        if (_selectedArenaId < 0)
            _selectedArenaId = Mathf.Max(NetworkArenaLobbyManager.Singleton.arenas.Count - 1, 0);
        createLobbyArenaText.text = NetworkArenaLobbyManager.Singleton.arenas[_selectedArenaId].id;
    }

    public void CreateLobby()
    {
        if (!NetworkArenaLobbyManager.Singleton || !NetworkCommandsProcessor.Singleton)
            return;
        var arenaId = NetworkArenaLobbyManager.Singleton.arenas[_selectedArenaId].id;
        var lobbyBaseId = "Lobby";
        var lobbyIdIndex = 0;
        while (NetworkArenaLobbyManager.Singleton.lobbies.ContainsKey(lobbyBaseId + lobbyIdIndex.ToString()))
            lobbyIdIndex++;
        NetworkCommandsProcessor.Singleton.ClientSendCommand($"lobby create {lobbyBaseId + lobbyIdIndex.ToString()} {arenaId}");
    }
}