using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mirror;
using Telepathy;
using UnityEngine;
public class NetworkCommandsProcessor : NetworkBehaviour
{

    private const string clientLocalHelpMessage = @"Local commands:
zoomin     - Decrease main camera orthographic size
zoomout    - Increase main camera orthographic size
debugui    - Show/hide debug UI on screen
hidechat   - Disable chat messages in output field
showchat   - Enable chat messages in output field
clear      - Clear all command output

Loading server commands...";

    private const string adminNotNeededMessageText = "admin: not needed";
    private const string adminRejectedMessageText = "admin: rejected";
    private const string adminGrantedMessageText = "admin: granted";
    private const string permissionErrorMessageText = @"Command access denied.
You can attempt to gain permission with:
admin <password>

(Available only if the server has admin password enabled)";
    private const string clientToolsWithoutClientErrorMessageText = "Error: used client argument tools but client is null";
    private const string syntaxErrorMessageText = "Error: invalid syntax";
    private const string unknownCommandMessageText = "Error: unknown command";

    private const string helpCommandPattern = "^help [a-z]+$";
    // new ^([a-z]+)(?: ((?:(?:p-)?[a-zA-Z0-9_]+|-?[0-9]+(?:\.[0-9]+)?(?:,-?[0-9]+(?:\.[0-9]+)?)?|"[^"]*")(?: (?:(?:p-)?[a-zA-Z0-9_]+|-?[0-9]+(?:\.[0-9]+)?(?:,-?[0-9]+(?:\.[0-9]+)?)?|"[^"]*"))*))?(?: (-[a-z]+(?: -[a-z]+)*))?$
    // old ^([a-z]+)(?: ((?:(?:p-)?[a-zA-Z0-9_]+|-?[0-9]+(?:.[0-9]+)?(?:,-?[0-9]+(?:.[0-9]+)?)?)(?: (?:(?:p-)?[a-zA-Z0-9_]+|-?[0-9]+(?:.[0-9]+)?(?:,-?[0-9]+(?:.[0-9]+)?)?))*))?(?: (-[a-z]+(?: -[a-z]+)*))?$
    private const string commandPattern = @"^([a-z]+)(?: ((?:(?:p-)?[a-zA-Z0-9_]+|-?[0-9]+(?:\.[0-9]+)?(?:,-?[0-9]+(?:\.[0-9]+)?)?|""[^""]*"")(?: (?:(?:p-)?[a-zA-Z0-9_]+|-?[0-9]+(?:\.[0-9]+)?(?:,-?[0-9]+(?:\.[0-9]+)?)?|""[^""]*""))*))?(?: (-[a-z]+(?: -[a-z]+)*))?$";

    public readonly SyncList<int> allowedConnections = new();

    private static bool TryResolveCommandInput(string textInput, out CommandInput resultCommandInput)
    {
        var match = Regex.Match(textInput, commandPattern);
        if (match.Success)
        {
            try
            {
                string[] args = Array.Empty<string>();
                if (match.Groups[2].Value.Length > 0)
                {
                    var argsMatches = Regex.Matches(match.Groups[2].Value, @"(?<=^|\s)(?:""([^""]*)""|(\S+))");
                    args = argsMatches
                        .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                        .ToArray();
                }
                resultCommandInput = new CommandInput()
                {
                    name = match.Groups[1].Value,
                    arguments = args,
                    flags = match.Groups[3].Value.Length > 0 ? match.Groups[3].Value.Split("-")[1..].Select(f => f.Trim()).ToArray() : Array.Empty<string>()
                };
                return true;
            }
            catch
            {
                resultCommandInput = new CommandInput();
                return false;
            }
        }
        resultCommandInput = new CommandInput();
        return false;
    }

    public static NetworkCommandsProcessor Singleton;

    private bool _serverAdminPasswordEnabled;
    private string _serverAdminPassword;

    private int _clientLastRequestId = -1;

    private readonly Dictionary<string, CommandObject> _commands = new();

    private string _helpMessage;

    public event Action<int, string> ClientOnCommandResponse;
    public event Action<string> ClientOnLocalCommandResponse;

    NetworkCommandsProcessor()
    {
        Singleton = this;
    }

    private void Awake()
    {
        GameCommandsLoader.LoadCommandsToDictionary(_commands);
        var helpStringBuilder = new StringBuilder();
        helpStringBuilder.AppendLine("help:");
        helpStringBuilder.AppendLine();
        helpStringBuilder.AppendLine("Command syntax:");
        helpStringBuilder.AppendLine("command <arguments> <flags>");
        helpStringBuilder.AppendLine("example: setstat 0 speedMultiplier 0.25 -inc");
        helpStringBuilder.AppendLine();
        helpStringBuilder.AppendLine("Recently used arguments:");
        helpStringBuilder.AppendLine("\'Target\' argument:");
        helpStringBuilder.AppendLine("netId or plrId or plrName (plrId starts with 'p')");
        helpStringBuilder.AppendLine("netId example: 10");
        helpStringBuilder.AppendLine("plrId example: p13");
        helpStringBuilder.AppendLine("plrName example: \"Player Name\"");
        helpStringBuilder.AppendLine("\'Position\' argument:");
        helpStringBuilder.AppendLine("syntax (x and y are float numbers): <x>,<y>");
        helpStringBuilder.AppendLine("position example: -12.34,-56.78");
        helpStringBuilder.AppendLine();
        helpStringBuilder.AppendLine("Client argument tools:");
        helpStringBuilder.AppendLine("@s    - Clietns current player id");
        helpStringBuilder.AppendLine();
        helpStringBuilder.AppendLine("Server commands:");
        helpStringBuilder.AppendLine("help  - Show command list");
        helpStringBuilder.AppendLine("help <command>  - Show command info");
        foreach (var command in _commands.Values)
            helpStringBuilder.AppendLine(command.shortHelp);
        _helpMessage = helpStringBuilder.ToString();
        Singleton = this;
    }

    private void OnDestroy()
    {
        ClientOnCommandResponse = null;
        ClientOnLocalCommandResponse = null;
    }

    public override void OnStartServer()
    {
        GameNetworkManager.Singleton.ServerOnPlayerLeave += ServerRemoveAllowedConnection;
        NetworkServer.RegisterHandler<CommandRequestMessage>(ServerHandleCommandRequestMessage);
        var configParams = GameNetworkManager.Singleton.serverConfig.GetConfigParams();
        _serverAdminPasswordEnabled = configParams.adminPasswordEnabled;
        _serverAdminPassword = configParams.adminPassword.Trim();
    }

    public override void OnStopServer()
    {
        GameNetworkManager.Singleton.ServerOnPlayerLeave -= ServerRemoveAllowedConnection;
        NetworkServer.UnregisterHandler<CommandRequestMessage>();
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<CommandResponseMessage>(ClientHandleCommandResponseMessage);
        if (NetworkServer.localConnection != null && !allowedConnections.Contains(NetworkServer.localConnection.connectionId))
            allowedConnections.Add(NetworkServer.localConnection.connectionId);
    }

    public override void OnStopClient()
    {
        NetworkClient.UnregisterHandler<CommandResponseMessage>();
    }

    [Server]
    private void ServerRemoveAllowedConnection(NetworkConnectionToClient conn)
    {
        allowedConnections.Remove(conn.connectionId);
    }

    [Server]
    private void ServerHandleCommandRequestMessage(NetworkConnectionToClient conn, CommandRequestMessage requestMessage)
    {
        string logOutput;
        string output;
        if (conn.isReady || conn.isAuthenticated)
        {
            if (ServerTryResolveAdminCommand(conn, requestMessage.text, out output))
            {
                logOutput = output;
            }
            else
            {
                if (allowedConnections.Contains(conn.connectionId))
                    output = ServerProceedCommand(conn, requestMessage.text, out logOutput);
                else
                {
                    output = permissionErrorMessageText;
                    logOutput = output;
                }
            }
            conn.Send(new CommandResponseMessage(requestMessage.requestId, output));
        }
        else
            logOutput = "no output: connection is not authenticated";
        if (logOutput != "")
            Debug.Log($"Player command input: connId: {conn.connectionId}; name: {conn.authenticationData}; input: {requestMessage.text}; clientRequestId: {requestMessage.requestId}; output: {logOutput}");
    }

    [Server]
    private bool ServerTryResolveAdminCommand(NetworkConnectionToClient conn, string textInput, out string output)
    {
        output = "";
        if (textInput.StartsWith("admin"))
        {
            if (allowedConnections.Contains(conn.connectionId))
                output = adminNotNeededMessageText;
            else if (!_serverAdminPasswordEnabled)
                output = adminRejectedMessageText;
            else
            {
                var granted = textInput[5..].Trim() == _serverAdminPassword;
                if (granted)
                    allowedConnections.Add(conn.connectionId);
                output = granted ? adminGrantedMessageText : adminRejectedMessageText;
            }
        }
        return output != "";
    }

    [Server]
    private bool ServerTryResolveHelpCommand(string textInput, out string output)
    {
        if (textInput == "help")
        {
            output = _helpMessage.MarkInfoCommandOutput();
            return true;
        }
        else if (textInput.StartsWith("help "))
        {
            if (!Regex.IsMatch(textInput, helpCommandPattern))
            {
                output = $"help: invalid syntax, printing default help\n${_helpMessage}".MarkInfoCommandOutput();
                return true;
            }
            var helpCommandName = textInput[5..];
            if (_commands.TryGetValue(helpCommandName, out var commandToHelp))
                output = (commandToHelp.fullHelp + (commandToHelp.additionalFullHelp?.Invoke() ?? "")).MarkInfoCommandOutput();
            else
                output = $"help: {helpCommandName} - Command not found".MarkErrorCommandOutput();
            return true;
        }
        output = "";
        return false;
    }

#nullable enable
    [Server]
    public string ServerProceedCommand(NetworkConnectionToClient? sender, string input, out string logOutput)
    {
        logOutput = "";
        var formattedInput = Regex.Replace(input.Trim(), @"\s+", " ");

        var funOutput = ServerTryResolveFunCommand(sender, formattedInput, ref logOutput);
        if (funOutput != null)
        {
            logOutput = funOutput[..Mathf.Min(75, funOutput.Length)] + (funOutput.Length > 75 ? $"...\n(original output length: {funOutput.Length})" : "");
            return funOutput;
        }

        if (formattedInput.Contains("@s"))
        {
            if (sender == null)
                return clientToolsWithoutClientErrorMessageText.MarkErrorCommandOutput();
            formattedInput = formattedInput.Replace("@s", $"p{sender.connectionId}");
        }
        if (ServerTryResolveHelpCommand(formattedInput, out var helpOutput))
        {
            logOutput = "(help output)";
            return helpOutput;
        }
        if (!TryResolveCommandInput(formattedInput, out var commandInput))
        {
            logOutput = syntaxErrorMessageText;
            return syntaxErrorMessageText.MarkErrorCommandOutput();
        }
        if (!_commands.TryGetValue(commandInput.name, out var command))
        {
            logOutput = unknownCommandMessageText;
            return unknownCommandMessageText.MarkErrorCommandOutput();
        }
        var output = command.execute(commandInput.arguments, commandInput.flags).Trim();
        logOutput = output[..Mathf.Min(75, output.Length)] + (output.Length > 75 ? $"...\n(original output length: {output.Length})" : "");
        if (command.hideFromDebugLog)
        {
            logOutput = "";
        }
        return output;
    }

    [Server]
    private string? ServerTryResolveFunCommand(NetworkConnectionToClient? sender, string input, ref string logOutput)
    {
        if (input == "praying for you o great mita")
        {
            var position = sender != null && sender.identity ? sender.identity.transform.position : Vector3.zero;
            var team = sender != null && sender.identity && sender.identity.TryGetComponent(out NetworkTank t) ? t.Parameters.TeamId : null;
            team ??= GameTanksManager.Singleton.teams[0].id;
            var tankId = GameTanksManager.Singleton.defaultPlayerTank;
            var tank = GameTanksManager.Singleton.ServerSpawnTank(teamId: team, tankId: tankId, position: position);
            tank.Parameters.DisplayName = "Praying for you O Great Mita";
            tank.Parameters.SpeedMultiplier = 1.5f;
            tank.Parameters.DamageMultiplier = 1.5f;
            tank.Parameters.ReloadMultiplier = 0.75f;
            tank.Parameters.MaxHp = tank.Parameters.MaxHp.Add(tank.Parameters.MaxHp / 2);
            tank.Parameters.CurrentHp = tank.Parameters.MaxHp;
            var funAiSettings = Resources.Load("Fun/FunTankAiSettings");
            var funScannerSettings = Resources.Load("Fun/FunTankAiScannerSettings");
            if (funAiSettings is TankAISettings)
                tank.ControlManager.tankAISettings = funAiSettings as TankAISettings;
            if (funScannerSettings is TankAIScannerSettings)
                tank.ControlManager.scannerSettings = funScannerSettings as TankAIScannerSettings;
            logOutput = "Praying for you O Great Mita";
            return "Praying for you O Great Mita";
        }
        return null;
    }
#nullable disable

    [Client]
    private void ClientHandleCommandResponseMessage(CommandResponseMessage message)
    {
        ClientOnCommandResponse?.Invoke(message.requestId, message.text);
    }

    [Client]
    private bool ClientTryResolveLocalCommand(string input)
    {
        switch (input.Trim())
        {
            case "debugui":
                var debugEnabled = TaknDebugDisplay.IsDebugDisplayEnabled && ClientDebugUI.IsDebugUIEnabled;
                debugEnabled = !debugEnabled;
                TaknDebugDisplay.IsDebugDisplayEnabled = debugEnabled;
                ClientDebugUI.IsDebugUIEnabled = debugEnabled;
                return true;
            case "hidechat":
                ChatHistoryUI.GlobalHideChatHistory();
                return true;
            case "showchat":
                ChatHistoryUI.GlobalShowChatHistory();
                return true;
            case "clear":
                ChatHistoryUI.GlobalClearCommandOutput();
                return true;
            case "zoomin":
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - 0.5f, 1, 100);
                return true;
            case "zoomout":
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize + 0.5f, 1, 100);
                return true;
            // Fun commands below...
            case "dimkadimkin08":
                ClientOnLocalCommandResponse?.Invoke("Hello");
                return true;
            case "ihateyouihateyougodie":
                ClientOnLocalCommandResponse?.Invoke("Correct password");
                return true;
            case "omo":
                ClientOnLocalCommandResponse?.Invoke("Welcome to white space... or rather, welcome to the command line space");
                return true;
            case "wonderhoy":
                ClientOnLocalCommandResponse?.Invoke("Wonderhoy!");
                return true;
            case "wot":
                ClientOnLocalCommandResponse?.Invoke("есть пробитие");
                return true;
            case "sus":
                ClientOnLocalCommandResponse?.Invoke("AMOGUS");
                return true;
            case "toby":
                var dog = GameObject.Find("your-worst-nightmare");
                if (!dog)
                {
                    dog = new GameObject("your-worst-nightmare");
                    var spriteRenderer = dog.AddComponent(typeof(SpriteRenderer)) as SpriteRenderer;
                    var res = Resources.Load("Fun/your-worst-nightmare");
                    if (res is Texture2D)
                    {
                        var texture = res as Texture2D;
                        dog.layer = LayerMask.NameToLayer("UI");
                        spriteRenderer.sortingLayerName = "Interface";
                        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
                    }
                }
                dog.transform.localScale = Vector3.one * UnityEngine.Random.Range(5f, 8f);
                dog.transform.position = UnityEngine.Random.insideUnitCircle * 300;
                ClientOnLocalCommandResponse?.Invoke("the dog does not exist the dog does not exist the dog does not exist");
                return true;
        }
        return false;
    }

    [Client]
    public int ClientSendCommand(string input)
    {
        if (ClientTryResolveLocalCommand(input))
            return -1;
        _clientLastRequestId++;
        if (input.Trim() == "help")
            ClientOnCommandResponse?.Invoke(_clientLastRequestId, clientLocalHelpMessage);
        NetworkClient.Send(new CommandRequestMessage(_clientLastRequestId, input));
        return _clientLastRequestId;
    }

    [Serializable]
    public struct CommandRequestMessage : NetworkMessage
    {
        public int requestId;
        public string text;

        public CommandRequestMessage(int requestId, string text)
        {
            this.requestId = requestId;
            this.text = text;
        }
    }

    [Serializable]
    public struct CommandResponseMessage : NetworkMessage
    {
        public int requestId;
        public string text;

        public CommandResponseMessage(int requestId, string text)
        {
            this.requestId = requestId;
            this.text = text;
        }
    }

    private struct CommandInput
    {
        public string name;
        public string[] arguments;
        public string[] flags;
    }
}
