using Mirror;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;
public class PlayerListUI : MonoBehaviour
{

    private static string ServerPlayerToString(NetworkPlayerList.Player player) => "- " + player.name;

    [SerializeField]
    private Text titleText;
    [SerializeField]
    private LocalizedString titleLocalizedString;
    [SerializeField]
    private TMP_InputField serverPlayerListText;
    [SerializeField]
    private RectTransform playerListTextRect;
    [SerializeField]
    private DynamicFontSize dynamicFontSize;

    private int _screenHeight;
    private int _screenWidth;

    private void Start()
    {
        RefreshServerPlayerListText();
        titleLocalizedString.ValueChanged += (v) => {
            titleText.text = v.ToString()
                .Replace("{playersCount}", NetworkPlayerList.Singleton.Players.Count.ToString()); };
    }

    private void OnEnable()
    {
        NetworkPlayerList.Singleton.Players.OnChange += OnPlayerListChanged;
        RefreshServerPlayerListText();
    }

    private void OnDisable()
    {
        NetworkPlayerList.Singleton.Players.OnChange -= OnPlayerListChanged;
    }

    private void Update()
    {
        if (_screenHeight != Screen.height || _screenWidth != Screen.width)
        {
            _screenHeight = Screen.height;
            _screenWidth = Screen.width;
            UpdateTextSize();
        }
    }

    private void OnPlayerListChanged(SyncIDictionary<int, NetworkPlayerList.Player>.Operation operation, int key, NetworkPlayerList.Player item)
    {
        RefreshServerPlayerListText();
    }

    private void UpdateTextSize()
    {
        serverPlayerListText.pointSize = dynamicFontSize.GetFontSize();
        playerListTextRect.sizeDelta = new Vector2(0, serverPlayerListText.preferredHeight);
    }

    private void RefreshServerPlayerListText()
    {
        if (titleText)
            titleLocalizedString.LoadWithCallback(result => {
                titleText.text = result
                    .Replace("{playersCount}", NetworkPlayerList.Singleton.Players.Count.ToString());
            });
        serverPlayerListText.text = string.Join("\n\n", NetworkPlayerList.Singleton.Players.Values.Select(ServerPlayerToString));
        UpdateTextSize();
    }
}
