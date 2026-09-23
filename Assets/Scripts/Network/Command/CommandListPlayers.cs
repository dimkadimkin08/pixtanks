using System.Linq;

public static class CommandListPlayers
{
    public static CommandObject commandObject = new()
    {
        name = "listplayers",
        shortHelp = "listplayers    - List players info",
        fullHelp = @"
listplayers - Disconnect player from server

Syntax:
listplayers",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 0 || flags.Length != 0)
            return "listplayers: invalid syntax".MarkErrorCommandOutput();

        var result = string.Join('\n', NetworkPlayerList.Singleton.Players.Values
            .OrderBy(player => player.id)
            .Select(player => $"player {player.id}: (tankId: {(CommandUtils.TryGetPlayerTankId(player.id, out var tankId) ? tankId : "none")}) (name: {player.name})"));

        return $"listplayers:\n{result}".MarkInfoCommandOutput();
    }
}