using System.Linq;
using System.Text;
using UnityEngine;

public static class CommandListTanks
{
    private static readonly string[] allowedFlags = new string[] { "player", "nonplayer", "nearby" };

    public static CommandObject commandObject = new()
    {
        name = "listtanks",
        shortHelp = "listtanks  - Get list of tanks on the server",
        fullHelp = @"
listtanks - Get list of tanks on the server

Syntax:
listtanks <observer> <flags>

Args:
<observer>  - Tank netId or player for -nearby flag

Flags:
-nearby     - Filter tanks nearby <observer>
-player     - Filter player tanks
-nonplayer  - Filter non-player tanks",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length > 1 || flags.Length > 0 && !flags.IsFits(allowedFlags))
            return "listtanks: invalid syntax".MarkErrorCommandOutput();

        NetworkTank observerTank = null;
        if (args.Length == 1 && !CommandUtils.TryGetTankFromArgument(args[0], out observerTank))
            return $"listtanks: cannot find <observer>: {args[0]}".MarkErrorCommandOutput();

        bool observerIsPlayer = observerTank != null && observerTank.connectionToClient != null;
        var visRange = !observerIsPlayer ? GameNetworkManager.Singleton.interestManagement.visRange : 0;

        var filterPlayer = flags.Contains("player");
        var filterNonPlayer = flags.Contains("nonplayer");
        var filterNearby = flags.Contains("nearby");

        if (filterNearby && observerTank == null)
            return "listtanks: error: <observer> argument required for using -nearby".MarkErrorCommandOutput();

        if (filterPlayer && filterNonPlayer)
            return "listtanks: error: cannot use -player with -nonplayer".MarkErrorCommandOutput();

        var result = new StringBuilder();
        result.Append($"listtanks");
        if (observerTank)
            result.Append($" (observer: {observerTank.netId})");
        result.Append($" ({string.Join(", ", flags)})");
        result.AppendLine(":");
        int tanksCount = 0;

        foreach (var tank in GameTanksManager.Singleton.ServerGetTankInstances().Where(tank => !tank.Death.IsDead))
        {
            if (filterNearby)
            {
                if (observerIsPlayer && !tank.netIdentity.observers.ContainsKey(observerTank.connectionToClient.connectionId))
                    continue;
                else if (!observerIsPlayer && visRange > Vector2.Distance(tank.transform.position, observerTank.transform.position))
                    continue;
            }
            if (filterPlayer && tank.connectionToClient == null)
                continue;
            if (filterNonPlayer && tank.connectionToClient != null)
                continue;

            result.Append(tank.name.Replace("(Clone)", ""));

            result.Append($" (id: {tank.netId})");

            if (tank.connectionToClient != null)
                result.Append($" (plr: {tank.connectionToClient.connectionId})");

            if (tank.Parameters.DisplayName.Length > 0)
                result.Append($" (name: {tank.Parameters.DisplayName})");

            result.Append($" (control: {CommandUtils.GetControllerName(tank)})");

            result.AppendLine();
            tanksCount++;
        }
        return result.ToString().MarkInfoCommandOutput();
    }
}