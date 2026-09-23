public static class CommandSpawnPoint
{
    public static CommandObject commandObject = new()
    {
        name = "spawnp",
        shortHelp = "spawnp    - Set player spawn position",
        fullHelp = @"
spawnp - Set player spawn position

Syntax:
spawnp <target> <position> <team> <delay>

or

spawnp <target> clear

Args:
<target>   - Player id or name
<position> - Position or tank (netId or plrId)
<team>     - Team id
<delay>    - (Optional) Respawn delay in seconds

Position format: <x>,<y>
(example: 12.34,-56.78)",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if ((args.Length != 2 && args.Length != 3 && args.Length != 4) || flags.Length != 0)
            return "spawnp: invalid syntax".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetPlayerFromArgument(args[0], out var player))
            return $"spawnp: cannot find <target>: {args[0]}".MarkErrorCommandOutput();

        if (args[1] == "clear")
        {
            GameNetworkManager.Singleton.tanksManager.ClearPlayerSpawnPoint(player);
            return $"spawnp (player: {player.connectionId}): spawn point deleted".MarkSuccessCommandOutput();
        }

        if (!CommandUtils.TryGetDestinationFromArgument(args[1], out var position))
            return $"spawnp: cannot resolve <position>: {args[1]}\n(must be tank netId or plrId or position in format <x>,<y> like 12.34,-56.78)"
                .MarkErrorCommandOutput();

        var team = GameTanksManager.Singleton.GetTeam(args[2]);
        if (team == default)
            return $"spawnp: cannot find <team>: {args[2]}".MarkErrorCommandOutput();

        float respawnDelay = -1;
        if (args.Length == 4 && !args[3].TryParseNoLocale(out respawnDelay))
            return $"spawnp: cannot parse float <delay>: {args[3]}".MarkErrorCommandOutput();

        GameNetworkManager.Singleton.tanksManager.SetPlayerSpawnPoint(player, position, team.id, respawnDelay);

        return $"spawnp (player: {player.connectionId}): spawn position set to {position}".MarkSuccessCommandOutput();
    }
}