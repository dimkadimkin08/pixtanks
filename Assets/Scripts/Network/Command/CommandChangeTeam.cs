using System.Linq;

public static class CommandChangeTeam
{
    private static string Teams => string.Join('\n', GameNetworkManager.Singleton.tanksManager.teams.Select(tank => tank.id));

    public static CommandObject commandObject = new()
    {
        name = "chteam",
        shortHelp = "chteam    - Change team of any tank",
        fullHelp = @"
chteam - Change team of any tank

Syntax:
chteam <target> <team>

Args:
<target>      - Tank netId or plrId
<team>        - Team name",
        additionalFullHelp = () => @$"

Teams:
{Teams}",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 2 || flags.Length > 0)
            return "chteam: invalid syntax".MarkErrorCommandOutput();

        var tanksManager = GameNetworkManager.Singleton.tanksManager;

        var tankTeam = args[1];

        if (tanksManager.GetTeam(tankTeam) == default)
            return $"chteam: cannot find <team>: {tankTeam}".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetTankFromArgument(args[0], out var tank))
            return $"chteam: cannot find <target>: {args[0]}".MarkErrorCommandOutput();

        tank.Parameters.TeamId = tankTeam;

        return $"chteam (tank: {tank.netId}): team set to {tankTeam}".MarkSuccessCommandOutput();
    }
}