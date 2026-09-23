using System.Linq;

public static class CommandKill
{
    private static readonly string[] allowedFlags = new string[] { "noloot" };

    public static CommandObject commandObject = new()
    {
        name = "kill",
        shortHelp = "kill    - Invoke tank death",
        fullHelp = @"
kill - Invoke tank death (without changing hp)

Syntax:
kill <target> <flags>

Args:
<target>   - Tank netId or plrId

Flags:
-noloot    - Do not create loot",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 1 || !flags.IsFits(allowedFlags))
            return "kill: invalid syntax".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetTankFromArgument(args[0], out var tank))
            return $"kill: cannot find <target>: {args[0]}".MarkErrorCommandOutput();

        if (tank.Death.IsDead)
            return $"kill (tank: {tank.netId}): tank already dead".MarkInfoCommandOutput();

        tank.Death.ServerKillTank(preventLootDrop: flags.Contains("noloot"));

        return $"kill: killed tank: {tank.netId}".MarkSuccessCommandOutput();
    }
}