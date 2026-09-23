using System.Collections.Generic;
public static class GameCommandsLoader
{
    public static void LoadCommandsToDictionary(Dictionary<string, CommandObject> commands)
    {
        commands[CommandListTanks.commandObject.name] = CommandListTanks.commandObject;
        commands[CommandSetStat.commandObject.name] = CommandSetStat.commandObject;
        commands[CommandTp.commandObject.name] = CommandTp.commandObject;
        commands[CommandSpawn.commandObject.name] = CommandSpawn.commandObject;
        commands[CommandChangeTank.commandObject.name] = CommandChangeTank.commandObject;
        commands[CommandChangeTeam.commandObject.name] = CommandChangeTeam.commandObject;
        commands[CommandGetTank.commandObject.name] = CommandGetTank.commandObject;
        commands[CommandKick.commandObject.name] = CommandKick.commandObject;
        commands[CommandListPlayers.commandObject.name] = CommandListPlayers.commandObject;
        commands[CommandSetController.commandObject.name] = CommandSetController.commandObject;
        commands[CommandKill.commandObject.name] = CommandKill.commandObject;
        commands[CommandKillAll.commandObject.name] = CommandKillAll.commandObject;
        commands[CommandSpawners.commandObject.name] = CommandSpawners.commandObject;
        commands[CommandAddEffect.commandObject.name] = CommandAddEffect.commandObject;
        commands[CommandSpawnPoint.commandObject.name] = CommandSpawnPoint.commandObject;
        commands[CommandLobby.commandObject.name] = CommandLobby.commandObject;
        commands[CommandNoClip.commandObject.name] = CommandNoClip.commandObject;
        commands[CommandGameRule.commandObject.name] = CommandGameRule.commandObject;
        commands[CommandServerLog.commandObject.name] = CommandServerLog.commandObject;
    }
}