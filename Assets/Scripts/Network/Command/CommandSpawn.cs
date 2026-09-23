using System;
using System.Linq;

public static class CommandSpawn
{
    private static readonly string[] allowedFlags = new string[] { "noai", "land" };

    private static string Tanks => string.Join('\n', GameTanksManager.Singleton.tanks.Select(tank => tank.id));
    private static string Teams => string.Join('\n', GameTanksManager.Singleton.teams.Select(team => team.id));
    private static string Structures => string.Join('\n', GameStructuresManager.Singleton.structures.Select(structure => structure.id));
    private static string Items => string.Join('\n', GameItemsManager.Singleton.items.Select(item => item.id));

    public static CommandObject commandObject = new()
    {
        name = "spawn",
        shortHelp = "spawn    - Spawn a tank",
        fullHelp = @"
spawn - Spawn an entity at specified position
spawn list - Show list of all available entities and teams

Syntax:
spawn <entity> <team> <destination> <flags>

Args:
<entity>        - Entity id (tank or item or structure)
<team>        - (tanks only) Tank team id
<destination> - Spawn position: position or tank (netId or plrId or playerName)

Flags:
-noai         - (tanks only) disable aiController
-land         - (tanks and structures only) use landing capsule",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length == 1 && flags.Length == 0 && args[0] == "list")
            return $@"spawn (list):

Tanks:
{Tanks}

Teams:
{Teams}

Structures:
{Structures}

Items:
{Items}".MarkInfoCommandOutput();
        if ((args.Length != 3 && args.Length != 2) || !flags.IsFits(allowedFlags))
            return "spawn: invalid syntax".MarkErrorCommandOutput();

        if (GameTanksManager.Singleton.GetTank(args[0]) != default)
            return SpawnTank(args, flags);

        if (GameStructuresManager.Singleton.GetStructure(args[0]) != default)
            return SpawnStructure(args, flags);

        if (GameItemsManager.Singleton.GetItem(args[0]) != default)
            return SpawnItem(args, flags);

        return $"spawn: unknown <entity>: {args[0]}\n(Use 'help spawn' to view available entities)";
    }

    private static string SpawnTank(string[] args, string[] flags)
    {
        if (args.Length != 3)
            return "spawn (tank): invalid syntax".MarkErrorCommandOutput();

        var noAi = flags.Contains("noai");
        var land = flags.Contains("land");
        var tankType = args[0];
        var tankTeam = args[1];

        var tanksManager = GameNetworkManager.Singleton.tanksManager;

        if (tanksManager.GetTeam(tankTeam) == default)
            return $"spawn (tank): cannot find <team>: {tankTeam}".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetDestinationFromArgument(args[2], out var destination))
            return $"spawn (tank): cannot resolve <destination>: {args[2]}\n(must be tank netId or plrId or position in format <x>,<y> like 12.34,-56.78)"
                .MarkErrorCommandOutput();

        if (!land)
        {
            var tank = tanksManager.ServerSpawnTank(tankId: tankType, teamId: tankTeam, position: destination, noAi: noAi);

            if (tank == null || !tank)
                return "spawn (tank): failed to spawn a tank".MarkErrorCommandOutput();

            return $"spawn (tank): created a tank with netId: {tank.netId}".MarkSuccessCommandOutput();
        }
        else
        {
            tanksManager.ServerSpawnBotLandingCapsule(
                tankId: tankType,
                teamId: tankTeam,
                position: destination,
                callback: (tank) => { },
                onError: () => { },
                noAi: noAi
            );
            return $"spawn (tank): landing capsule requested to position: {destination}".MarkSuccessCommandOutput();
        }
    }

    private static string SpawnStructure(string[] args, string[] flags)
    {
        if (args.Length != 2)
            return "spawn (structure): invalid syntax".MarkErrorCommandOutput();

        var land = flags.Contains("land");
        var structureId = args[0];
        
        var structuresManager = GameStructuresManager.Singleton;

        if (!CommandUtils.TryGetDestinationFromArgument(args[1], out var destination))
            return $"spawn (structure): cannot resolve <destination>: {args[1]}\n(must be tank netId or plrId or position in format <x>,<y> like 12.34,-56.78)"
                .MarkErrorCommandOutput();

        if (!land)
        {
            var structure = structuresManager.ServerSpawnStructure(structureId: structureId, position: destination);

            if (structure == null || !structure)
                return "spawn (structure): failed to spawn a structure".MarkErrorCommandOutput();

            return $"spawn (structure): created a structure with netId: {structure.netId}".MarkSuccessCommandOutput();
        }
        else
        {
            structuresManager.ServerSpawnStructureLandingCapsule(
                structureId: structureId,
                position: destination,
                callback: (structure) => { },
                onError: () => { }
            );
            return $"spawn (structure): landing capsule requested to position: {destination}".MarkSuccessCommandOutput();
        }
    }

    private static string SpawnItem(string[] args, string[] flags)
    {
        if (args.Length != 2)
            return "spawn (item): invalid syntax".MarkErrorCommandOutput();

        var itemId = args[0];

        var itemsManager = GameItemsManager.Singleton;

        if (!CommandUtils.TryGetDestinationFromArgument(args[1], out var destination))
            return $"spawn (item): cannot resolve <destination>: {args[1]}\n(must be tank netId or plrId or position in format <x>,<y> like 12.34,-56.78)"
                .MarkErrorCommandOutput();

        var item = itemsManager.ServerSpawnItem(itemId: itemId, position: destination, impulse: UnityEngine.Vector2.zero, impulseDuration: 0);

        if (item == null || !item)
            return "spawn (item): failed to spawn an item".MarkErrorCommandOutput();

        return $"spawn (item): created an item with netId: {item.netId}".MarkSuccessCommandOutput();
    }
}