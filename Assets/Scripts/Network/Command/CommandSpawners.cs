using System;
using System.Linq;

public static class CommandSpawners
{
    private static readonly string[] allowedFlags = new string[] { "tanks", "structs", "loot" };
    public static CommandObject commandObject = new()
    {
        name = "spawners",
        shortHelp = "spawners    - Interact with spawners",
        fullHelp = @"
spawners - Interact with spawners (enable/disable or destroy spawned entities)

Syntax:
spawners <operation> <flags>

Args:
<operation> - Values: enable, disable, clear

Flags:
-tanks      - Affect only tank spawners
-structs    - Affect only structure spawners
-loot       - (for clear operation) enable loot drop for destroyed entities",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 1 || flags.Length > 0 && !flags.IsFits(allowedFlags))
            return "spawners: invalid syntax".MarkErrorCommandOutput();

        var tanksFlag = flags.Contains("tanks");
        var structsFlag = flags.Contains("structs");
        var lootFlag = flags.Contains("loot");

        var allSpawners = tanksFlag == structsFlag;
        var affectedSpawnersString = $"{ (!structsFlag ? "tanks" : "") }{ (allSpawners ? " and " : "")}{ (!tanksFlag ? "structures" : "")}";

        switch (args[0])
        {
            case "enable":
                if (!structsFlag)
                    foreach (var spawner in ServerTankSpawner.spawners)
                        spawner.spawnerEnabled = true;
                if (!tanksFlag)
                    foreach (var spawner in ServerStructureSpawner.spawners)
                        spawner.spawnerEnabled = true;
                return $"spawners: enabled spawners for {affectedSpawnersString}".MarkSuccessCommandOutput();
            case "disable":
                if (!structsFlag)
                    foreach (var spawner in ServerTankSpawner.spawners)
                        spawner.spawnerEnabled = false;
                if (!tanksFlag)
                    foreach (var spawner in ServerStructureSpawner.spawners)
                        spawner.spawnerEnabled = false;
                return $"spawners: disabled spawners for {affectedSpawnersString}".MarkSuccessCommandOutput();
            case "clear":
                var tanksCount = 0;
                var structsCount = 0;
                if (!structsFlag)
                    foreach (var spawner in ServerTankSpawner.spawners)
                    {
                        tanksCount += spawner.spawnedTanks.Count;
                        spawner.ClearSpawner(lootEnabled: lootFlag);
                    }
                if (!tanksFlag)
                    foreach (var spawner in ServerStructureSpawner.spawners)
                    {
                        structsCount += spawner.spawnedStructures.Count;
                        spawner.ClearSpawner(lootEnabled: lootFlag);
                    }
                return $"spawners: destroyed {tanksCount} tanks and {structsCount} structures (loot: {lootFlag})".MarkSuccessCommandOutput();
            default:
                return $"spawners: unknown <operation>: {args[0]}\n(Allowed operations: enable, disable, clear)".MarkErrorCommandOutput();
        }
    }
}