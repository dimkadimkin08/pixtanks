public static class CommandSetController
{
    public static CommandObject commandObject = new()
    {
        name = "setcontroller",
        shortHelp = "setcontroller    - Set tank controller",
        fullHelp = @"
setcontroller - Set tank controller (player or ai or none)

Syntax:
setcontroller <target> <controller>

Args:
<target>      - Tank netId or plrId
<controller>  - Controller name

Controllers:
player
ai
none",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length != 2 || flags.Length != 0)
            return "setcontroller: invalid syntax".MarkErrorCommandOutput();

        if (!CommandUtils.TryGetTankFromArgument(args[0], out var tank))
            return $"setcontroller: cannot find <target>: {args[0]}".MarkErrorCommandOutput();

        var controllerName = args[1];
        switch (controllerName)
        {
            case "player":
                if (tank.connectionToClient != null)
                    tank.ControlManager.serverCurrentController = TankControlManager.TankController.Player;
                else
                    return $"setcontroller (tank: {tank.netId}): cannot set \'player\' controller for non-player tank".MarkErrorCommandOutput();
                break;
            case "ai":
                tank.ControlManager.serverCurrentController = TankControlManager.TankController.AI;
                break;
            case "none":
                tank.ControlManager.serverCurrentController = TankControlManager.TankController.None;
                break;
            default:
                return $"setcontroller (tank: {tank.netId}): cannot resolve <controller>: {controllerName}\n(allowed controllers: player, ai, none)"
                    .MarkErrorCommandOutput();
        }

        return $"setcontroller (tank: {tank.netId}): controller set to \'{controllerName}\'".MarkSuccessCommandOutput();
    }
}