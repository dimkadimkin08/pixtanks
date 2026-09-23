public static class CommandTp
{
    public static CommandObject commandObject = new()
    {
        name = "tp",
        shortHelp = "tp    - Teleport tank to specified destination",
        fullHelp = @"
tp - Teleport tank to specified destination

Syntax:
tp <target> <destination>

Args:
<target>      - Tank netId or plrId or player name
<destination> - Position or tank (netId or plrId or name)

Position format: <x>,<y>
(example: 12.34,-56.78)",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 2 || flags.Length != 0)
            return "tp: invalid syntax".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetTankFromArgument(args[0], out var tank))
            return $"tp: cannot find <target>: {args[0]}".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetDestinationFromArgument(args[1], out var destination))
            return $"tp: cannot resolve <destination>: {args[1]}\n(must be tank netId or plrId or position in format <x>,<y> like 12.34,-56.78)"
                .MarkErrorCommandOutput();

        tank.transform.position = destination;

        return $"tp (tank: {tank.netId}): position set to {destination}".MarkSuccessCommandOutput();
    }
}