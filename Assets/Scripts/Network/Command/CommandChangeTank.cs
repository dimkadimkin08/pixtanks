using System.Linq;

public static class CommandChangeTank
{
    private static string Tanks => string.Join('\n', GameNetworkManager.Singleton.tanksManager.tanks.Select(tank => tank.id));

    public static CommandObject commandObject = new()
    {
        name = "chtank",
        shortHelp = "chtank    - Change tank for player",
        fullHelp = @"
chtank - Change tank for player

Syntax:
chtank <player> <tank>

Args:
<player>      - Player id
<tank>        - Tank type name",
        additionalFullHelp = () => @$"

Tanks:
{Tanks}",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 2 || flags.Length > 0)
            return "chtank: invalid syntax".MarkErrorCommandOutput();

        var tanksManager = GameNetworkManager.Singleton.tanksManager;

        var tankType = args[1];

        if (tanksManager.GetTank(tankType) == default)
            return $"chtank: cannot find <tank>: {tankType}".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetPlayerFromArgument(args[0], out var conn))
            return $"chtank: cannot find <player>: {args[0]}".MarkErrorCommandOutput();

        if (!tanksManager.ServerReplacePlayerTank(conn: conn, tankId: tankType))
            return "chtank: failed to change tank".MarkErrorCommandOutput();

        return $"chtank: set {tankType} for player {conn.connectionId} (tank netId: {conn.identity.netId})".MarkSuccessCommandOutput();
    }
}