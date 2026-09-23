using System.Linq;
using UnityEngine;

public static class CommandKillAll
{
    public static CommandObject commandObject = new()
    {
        name = "killall",
        shortHelp = "killall    - Kill ALL tanks",
        fullHelp = @"
killall - Kill ALL tanks on the server (loot drop is forcibly disabled)

Syntax:
killall",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 0 || flags.Length != 0)
            return "killall: invalid syntax".MarkErrorCommandOutput();

        var tanks = GameTanksManager.Singleton.ServerGetTankInstances().Where(tank => !tank.Death.IsDead);
        var tanksCount = tanks.Count();

        if (tanksCount == 0)
            return "killall: no tanks to kill".MarkInfoCommandOutput();

        foreach (var tank in tanks)
            tank.Death.ServerKillTank(preventLootDrop: true);

        return @$"killall: killed {tanksCount} tanks {Random.Range(0, 100) switch
        {
            1 => " (My power is absolute!)",
            2 => " (You shall die!)",
            3 => " (I am the storm that is approaching!)",
            4 => " (Where is your motivation?)",
            5 => " (You are wasting my time...)",
            6 => " (Rest in peace...)",
            7 => " (Cut of!)",
            8 => " (This is the end!)",
            _ => ""
        }}".MarkSuccessCommandOutput();
    }
}