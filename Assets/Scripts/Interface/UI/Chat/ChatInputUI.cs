using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class ChatInputUI : MonoBehaviour
{
    private const int inputHistoryLimit = 100;

    public static bool IsInputVisible { get; private set; }

    public static event Action<bool> OnInputFocusSwitch;

    [SerializeField]
    private GameObject inputUIObject;
    [SerializeField]
    private InputField messageInput;

    private float _lastSentMessageTime;

    private bool _pendingMoveTextEnd;

    private readonly List<string> _inputHistory = new();
    private int _currentInputIndex = -1;

    private bool IsCooldown => NetworkChat.Singleton
        && Time.time - _lastSentMessageTime < NetworkChat.Singleton.MessageSendCooldown + NetworkTime.rtt + 0.1f;

    private void Start()
    {
        messageInput.characterLimit = NetworkChat.Singleton.MaxMessageLength;
        SetInputFocus(false);
    }

    private void OnDisable()
    {
        SetInputFocus(false);
        StopAllCoroutines();
    }

    private void Update()
    {
        if (IsInputVisible)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                ShowPreviousInput();
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                ShowNextInput();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            SetInputFocus(false);

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (IsInputVisible && (!IsCooldown || messageInput.text.Trim().Length == 0))
            {
                SetInputFocus(false);
                TrySendMessage();
            }
            else if (!IsInputVisible)
                SetInputFocus(true);
        }

        if (_pendingMoveTextEnd && IsInputVisible)
        {
            messageInput.MoveTextEnd(false);
            _pendingMoveTextEnd = false;
        }

        if (Keyboard.current.slashKey.wasPressedThisFrame && !IsInputVisible)
        {
            _pendingMoveTextEnd = true;
            SetInputFocus(true);
            messageInput.SetTextWithoutNotify("/");
        }
        else if (Keyboard.current.backslashKey.wasPressedThisFrame && !IsInputVisible)
        {
            _pendingMoveTextEnd = true;
            SetInputFocus(true);
            messageInput.SetTextWithoutNotify("\\");
        }
    }

    private void TrySendMessage()
    {
        var text = NetworkChat.FormatMessageText(messageInput.text);
        if (text.StartsWith('/') || text.StartsWith('\\'))
        {
            if (text.Length > 1)
            {
                messageInput.text = "";
                NetworkCommandsProcessor.Singleton.ClientSendCommand(text[1..]);
                AddToInputHistory(text);
                SetInputFocus(false);
            }
        }
        else if (!IsCooldown && text.Length > 0 && text.Length <= NetworkChat.Singleton.MaxMessageLength)
        {
            messageInput.text = "";
            NetworkChat.Singleton.SendMessageToChat(text);
            AddToInputHistory(text);
            _lastSentMessageTime = Time.time;
            SetInputFocus(false);
        }
    }

    private void AddToInputHistory(string input)
    {
        if (!string.IsNullOrEmpty(input) && (_inputHistory.Count == 0 || input != _inputHistory[^1]))
        {
            _inputHistory.Add(input);
            var extraInputCount = Math.Max(0, _inputHistory.Count - inputHistoryLimit);
            for (int i = 0; i < extraInputCount; i++)
                _inputHistory.RemoveAt(0);
        }
        _currentInputIndex = _inputHistory.Count;
    }

    private void ShowPreviousInput()
    {
        if (_currentInputIndex > 0)
        {
            _currentInputIndex--;
            messageInput.text = _inputHistory[_currentInputIndex];
            _pendingMoveTextEnd = true;
        }
    }

    private void ShowNextInput()
    {
        if (_currentInputIndex < _inputHistory.Count - 1)
        {
            _currentInputIndex++;
            messageInput.text = _inputHistory[_currentInputIndex];
            _pendingMoveTextEnd = true;
        }
        else
        {
            messageInput.text = "";
            _currentInputIndex = _inputHistory.Count;
            _pendingMoveTextEnd = true;
        }
    }

    public void SetInputFocus(bool isFocus)
    {
        IsInputVisible = !PauseMenuSwitch.PauseActive && isFocus;
        if (!IsInputVisible)
            messageInput.DeactivateInputField();
        inputUIObject.SetActive(IsInputVisible);
        if (IsInputVisible)
            messageInput.ActivateInputField();
        OnInputFocusSwitch?.Invoke(IsInputVisible);
    }

    public void SwitchInputFocus()
    {
        SetInputFocus(!IsInputVisible);
    }
}