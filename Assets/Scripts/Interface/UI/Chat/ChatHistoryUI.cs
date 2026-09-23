using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
public class ChatHistoryUI : MonoBehaviour
{
    private const string MessageTimestampFormat = "HH:mm (dd.MM.yyyy)";

    public static bool IsCollapsed;
    private static bool chatHistoryVisible = true;
    private static event Action OnHideChatHistory;
    private static event Action OnShowChatHistory;
    private static event Action OnClearCommandOutput;

    public static void GlobalClearCommandOutput()
    {
        OnClearCommandOutput?.Invoke();
    }

    public static void GlobalShowChatHistory()
    {
        chatHistoryVisible = true;
        OnShowChatHistory?.Invoke();
    }

    public static void GlobalHideChatHistory()
    {
        chatHistoryVisible = false;
        OnHideChatHistory?.Invoke();
    }

    [Header("Chat Display")]
    [SerializeField]
    private TextMeshProUGUI chatHistoryText;
    [SerializeField]
    private TMP_InputField chatHistoryTextMain;
    [SerializeField]
    private RectTransform chatHistoryTextMainRect;
    [SerializeField]
    private DynamicFontSize dynamicFontSize;
    [SerializeField]
    private float timestampScale = 1;
    [SerializeField]
    private ScrollRect scrollRect;

    [Header("Command Messages")]
    [SerializeField]
    private Color commandMessageColorNormal = Color.gray;
    [SerializeField]
    private Color commandMessageColorSuccess = new(0, 1, 0, 0.7f);
    [SerializeField]
    private Color commandMessageColorError = new(1, 0, 0, 0.7f);
    [SerializeField]
    private Color commandMessageColorInfo = new(1, 0.5f, 0, 0.7f);

    [Header("Collapse & Expand")]
    [SerializeField]
    private bool collapsedOnStart = true;
    [SerializeField]
    private RectTransform chatHistoryContainer;
    [SerializeField]
    private AnchoredSize expandedAnchoredSize;
    [SerializeField]
    private AnchoredSize collapsedAnchoredSize;

    private readonly List<NetworkChat.Message> _commandOutputMessages = new();

    private bool _chatInputFocused;
    private float _lastChatScale;
    private int _screenHeight;
    private int _screenWidth;

    private void Start()
    {
        _chatInputFocused = ChatInputUI.IsInputVisible;
        SetCollapsed(collapsedOnStart);
        RefreshChatHistoryText();
    }

    private void OnEnable()
    {
        OnClearCommandOutput += ClearCommandOutput;
        OnShowChatHistory += RefreshChatHistoryText;
        OnHideChatHistory += RefreshChatHistoryText;
        GameSettings.OnChatScaleChanged += OnChatScaleChanged;
        NetworkChat.Singleton.ClientOnMessagesChanged += RefreshChatHistoryText;
        NetworkCommandsProcessor.Singleton.ClientOnCommandResponse += OnCommandResponse;
        NetworkCommandsProcessor.Singleton.ClientOnLocalCommandResponse += OnLocalCommandResponse;
        ChatInputUI.OnInputFocusSwitch += OnChatInputFocusChanged;
    }

    private void OnDisable()
    {
        OnClearCommandOutput -= ClearCommandOutput;
        OnShowChatHistory -= RefreshChatHistoryText;
        OnHideChatHistory -= RefreshChatHistoryText;
        GameSettings.OnChatScaleChanged -= OnChatScaleChanged;
        NetworkChat.Singleton.ClientOnMessagesChanged -= RefreshChatHistoryText;
        NetworkCommandsProcessor.Singleton.ClientOnCommandResponse -= OnCommandResponse;
        NetworkCommandsProcessor.Singleton.ClientOnLocalCommandResponse -= OnLocalCommandResponse;
        ChatInputUI.OnInputFocusSwitch -= OnChatInputFocusChanged;
    }

    private void Update()
    {
        if (_screenHeight != Screen.height || _screenWidth != Screen.width)
        {
            _screenHeight = Screen.height;
            _screenWidth = Screen.width;
            UpdateTextSize(GameSettings.ChatScale);
        }
        if (GameSettings.ChatScale != _lastChatScale)
            OnChatScaleChanged(GameSettings.ChatScale);
        if (Keyboard.current.cKey.wasPressedThisFrame && !ChatInputUI.IsInputVisible)
            SetCollapsed(!IsCollapsed);
    }

    private void OnCommandResponse(int requestId, string text)
    {
        _commandOutputMessages.Add(new NetworkChat.Message
        {
            type = NetworkChat.MessageType.CommandOutput,
            time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
            text = text
        });
        RefreshChatHistoryText();
    }

    private void OnLocalCommandResponse(string text)
    {
        _commandOutputMessages.Add(new NetworkChat.Message
        {
            type = NetworkChat.MessageType.CommandOutput,
            time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
            text = text
        });
        RefreshChatHistoryText();
    }

    private void OnChatInputFocusChanged(bool chatInputFocus)
    {
        _chatInputFocused = chatInputFocus;
        ApplyCollapsedState();
    }

    private void OnChatScaleChanged(float chatScale)
    {
        _lastChatScale = chatScale;
        RefreshChatHistoryText();
    }

    private void RefreshChatHistoryText()
    {
        IOrderedEnumerable<NetworkChat.Message> messages;
        if (chatHistoryVisible)
            messages = NetworkChat.Singleton.Messages.Concat(_commandOutputMessages).OrderBy(message => message.time);
        else
            messages = _commandOutputMessages.OrderBy(message => message.time);
        chatHistoryTextMain.text = string.Join("\n\n", messages.Select(ChatMessageToString));
        UpdateTextSize(GameSettings.ChatScale);
    }

    private void UpdateTextSize(float chatScale)
    {
        chatHistoryText.fontSize = Mathf.RoundToInt(dynamicFontSize.GetFontSize() * chatScale);
        chatHistoryTextMainRect.sizeDelta = new Vector2(0, chatHistoryText.preferredHeight);
        scrollRect.verticalNormalizedPosition = 0;
        var timestampFontSizeText = $"<size={Mathf.RoundToInt(chatHistoryText.fontSize * timestampScale)}>";
        chatHistoryTextMain.text = Regex.Replace(chatHistoryTextMain.text, @"<size=.*?>", timestampFontSizeText);
    }

    private string FormatCommandOutputText(string commandOutputText)
    {
        var text = commandOutputText;
        var color = commandMessageColorNormal;
        if (commandOutputText.StartsWith("#ERROR#"))
        {
            color = commandMessageColorError;
            text = text[7..];
        }
        else if (commandOutputText.StartsWith("#SUCCESS#"))
        {
            color = commandMessageColorSuccess;
            text = text[9..];
        }
        else if (commandOutputText.StartsWith("#INFO#"))
        {
            color = commandMessageColorInfo;
            text = text[6..];
        }
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }

    private string ChatMessageToString(NetworkChat.Message message)
    {
        var time = DateTimeOffset.FromUnixTimeMilliseconds(message.time).ToLocalTime().DateTime.ToString(MessageTimestampFormat);
        var timeString = $"<size={Mathf.RoundToInt(chatHistoryText.fontSize * timestampScale)}>{time}</size>\n";
        var safeText = message.text.Replace("<", "<\u200B").Replace(">", "\u200B>");
        return message.type switch
        {
            NetworkChat.MessageType.UserMessage => timeString + message.username + ": " + safeText,
            NetworkChat.MessageType.CommandOutput => FormatCommandOutputText(safeText),
            _ => timeString + safeText
        };
    }

    private void Collapse()
    {
        SetCollapsed(true);
    }

    private void Expand()
    {
        SetCollapsed(false);
    }

    private void ClearCommandOutput()
    {
        _commandOutputMessages.Clear();
        RefreshChatHistoryText();
    }

    public void SetCollapsed(bool isCollapsed)
    {
        IsCollapsed = isCollapsed;
        ApplyCollapsedState();
    }

    private void ApplyCollapsedState()
    {
        bool actuallyCollapsed = !_chatInputFocused && IsCollapsed;

        var anchoredSize = actuallyCollapsed
            ? collapsedAnchoredSize
            : expandedAnchoredSize;

        chatHistoryContainer.anchorMin = anchoredSize.min;
        chatHistoryContainer.anchorMax = anchoredSize.max;

        scrollRect.verticalNormalizedPosition = 0;
        RefreshChatHistoryText();
    }
}
