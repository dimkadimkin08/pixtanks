using Mirror;

public static class CommandKick
{
    public static CommandObject commandObject = new()
    {
        name = "kick",
        shortHelp = "kick    - Disconnect player from server",
        fullHelp = @"
kick - Disconnect player from server

Syntax:
tp <player>

Args:
<player>      - Player id or name",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 1 || flags.Length != 0)
            return "kick: invalid syntax".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetPlayerFromArgument(args[0], out var conn))
            return $"kick: cannot find <player>: {args[0]}".MarkErrorCommandOutput();

        if (NetworkServer.activeHost
            && NetworkServer.localConnection != null
            && NetworkServer.localConnection.connectionId == conn.connectionId)
        {
            return $"kick: cannot disconnect host".MarkErrorCommandOutput();
        }
        else
            conn.Disconnect();

        return $"kick: disconnected player {conn.connectionId}".MarkSuccessCommandOutput();
    }
}