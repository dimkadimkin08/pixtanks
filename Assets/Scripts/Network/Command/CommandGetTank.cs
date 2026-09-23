using System.Linq;
using System.Text;
using Mirror;

public static class CommandGetTank
{
    public static CommandObject commandObject = new()
    {
        name = "gettank",
        shortHelp = "gettank  - Get information about tank",
        fullHelp = @"
gettank - Get information about tank

Syntax:
gettank <target>

Args:
<target> - Tank netId or plrId",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 1 || flags.Length > 0)
            return "gettank: invalid syntax".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetTankFromArgument(args[0], out var tank, requireAlive: false))
            return $"gettank: cannot find <target>: {args[0]}".MarkErrorCommandOutput();

        var result = new StringBuilder();

        result.AppendLine("gettank:");
        result.AppendLine($"netId: {tank.netId}");
        result.AppendLine($"plrId: {tank.connectionToClient?.connectionId}");
        var lifetime = System.Math.Round(NetworkTime.time - tank.Parameters.SpawnTimestamp);
        result.AppendLine($"lifetime: {lifetime} seconds");
        if (tank.Death.IsDead)
        {
            result.AppendLine($"tank is dead");
        }
        else
        {
            if (CommandUtils.TryGetTankType(tank, out var tankType))
                result.AppendLine($"tankType: {tankType}");
            else
                result.AppendLine($"tankType is unknown");
            result.AppendLine($"teamId: {tank.Parameters.TeamId}");
            result.AppendLine($"controller: {CommandUtils.GetControllerName(tank)}");
            result.AppendLine($"position: {tank.transform.position}");
            result.AppendLine($"displayName: {tank.Parameters.DisplayName}");
            result.AppendLine($"gears: {tank.Parameters.Gears}");
            result.AppendLine($"maxHp: {tank.Parameters.MaxHp}");
            result.AppendLine($"currentHp: {tank.Parameters.CurrentHp}");
            result.AppendLine($"speedMultiplier: {tank.Parameters.SpeedMultiplier}");
            result.AppendLine($"damageMultiplier: {tank.Parameters.DamageMultiplier}");
            result.AppendLine($"reloadMultiplier: {tank.Parameters.ReloadMultiplier}");
            result.AppendLine($"incomingDamageMultiplier: {tank.Parameters.IncomingDamageMultiplier}");
            result.AppendLine($"shieldsCount: {tank.Parameters.shields.Count}");
            result.AppendLine($"totalShieldValue: {tank.Parameters.shields.Sum(shield => shield.Value)}");
            foreach (var shield in tank.Parameters.shields)
                result.AppendLine($"shield_{shield.Key}: {shield.Value}");
        }

        return result.ToString().MarkInfoCommandOutput();
    }
}