using System;
using System.Linq;
using Mirror;
using UnityEngine;
using System.IO;
public class NetworkChat : NetworkBehaviour
{

    public static string FormatMessageText(string messageText) => messageText.Replace("\r", "").Replace("\n", "").Trim();

    public static NetworkChat Singleton;

    [SerializeField]
    private ushort maxMessageLength = 70;
    [SerializeField]
    private ushort maxMessages = 30;
    [SerializeField]
    private float messageSendCooldown = 2;

    public ushort MaxMessageLength => maxMessageLength;
    public float MessageSendCooldown => messageSendCooldown;

    public readonly SyncList<Message> Messages = new();

    public event Action ClientOnMessagesChanged;
    public event Action<int> ClientOnMessageAdd;

    public enum MessageType { UserMessage, CommandOutput }

    private string serverChatLogFile = "";

    public NetworkChat()
    {
        Singleton = this;
    }

    private void Awake()
    {
        Singleton = this;
    }

    private void OnDestroy()
    {
        ClientOnMessagesChanged = null;
        ClientOnMessageAdd = null;
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<SendMessageRequest>(ServerAddMessage);
        serverChatLogFile = GameNetworkManager.Singleton.serverConfig.GetConfigParams().serverChatLogFile;
        ServerTryRestoreChatHisotry();
    }

    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<SendMessageRequest>();
        ServerTrySaveChatHisotry();
    }

    public override void OnStartClient()
    {
        Messages.OnChange += (operation, index, message) => ClientOnMessagesChanged?.Invoke();
        Messages.OnAdd += (index) => ClientOnMessageAdd?.Invoke(index);
        ClientOnMessagesChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        Messages.OnChange = null;
        Messages.OnAdd = null;
    }

    [Server]
    private void RemoveExtraMessages()
    {
        var extraMessages = Messages.Count - maxMessages;
        for (var i = 1; i <= extraMessages; i++)
            Messages.RemoveAt(0);
    }

    [Server]
    private void ServerAddMessage(NetworkConnectionToClient sender, SendMessageRequest request)
    {
        if (!sender.isAuthenticated)
            return;

        var isMessageCooldown = Messages.Any(message =>
        {
            if (message.tankNetId != sender.connectionId)
                return false;
            var seconds = (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - message.time) / 1000.0;
            return seconds < messageSendCooldown;
        });
        if (isMessageCooldown)
            return;

        var formattedText = FormatMessageText(request.MessageText);
        formattedText = formattedText[..Mathf.Min(formattedText.Length, maxMessageLength)];
        if (formattedText.Length == 0)
            return;

        var message = new Message
        {
            type = MessageType.UserMessage,
            time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
            userId = sender.connectionId,
            tankNetId = sender.identity ? sender.identity.netId : 0,
            username = sender.Username(),
            text = formattedText
        };

        Messages.Add(message);

        Debug.Log($"Chat message (sender: ({message.userId}) {message.username}): {message.text}");

        RemoveExtraMessages();
        ServerTrySaveChatHisotry();
    }

    [Client]
    public void SendMessageToChat(string text)
    {
        NetworkClient.Send(new SendMessageRequest { MessageText = text });
    }

    [Server]
    private void ServerTryRestoreChatHisotry()
    {
        if (serverChatLogFile != "")
        {
            var filePath = $"{Directory.GetCurrentDirectory()}/{serverChatLogFile}";
            try
            {
                var hsitory = JsonUtility.FromJson<ChatHistory>(File.ReadAllText(filePath));
                Messages.InsertRange(0, hsitory.messages);
            }
            catch
            {
                Debug.Log($"Failed to restore chat history from serverChatLogFile: {filePath}");
            }
        }
    }

    [Server]
    private void ServerTrySaveChatHisotry()
    {
        if (serverChatLogFile != "")
        {
            var filePath = $"{Directory.GetCurrentDirectory()}/{serverChatLogFile}";
            try
            {
                var hisotry = new ChatHistory() { messages = Messages.ToArray() };
                File.WriteAllText(filePath, JsonUtility.ToJson(hisotry, true));
            }
            catch
            {
                Debug.Log($"Failed to save chat history to serverChatLogFile: {filePath}");
            }
        }
    }

    private struct SendMessageRequest : NetworkMessage
    {
        public string MessageText;
    }

    [Serializable]
    public struct Message
    {
        public MessageType type;
        public long time;
        public int userId;
        public uint tankNetId;
        public string username;
        public string text;
    }

    [Serializable]
    private struct ChatHistory
    {
        public Message[] messages;
    }

}
