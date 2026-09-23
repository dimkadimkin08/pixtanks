using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
public static class CommandLobby
{
    private static NetworkArenaLobbyManager LobbyManager => NetworkArenaLobbyManager.Singleton;

    public static CommandObject commandObject = new()
    {
        name = "lobby",
        shortHelp = "lobby  - Interact with arena lobbies",
        fullHelp = @"
lobby - Interact with arena lobbies

Syntax:
lobby arenalist
lobby list
lobby create <lobbyId> <arenaId>
lobby info <lobbyId>
lobby start <lobbyId>
lobby restart <lobbyId>
lobby stop <lobbyId>
lobby delete <lobbyId>
lobby reset <lobbyId>
lobby join <lobbyId> <player> <team>
lobby leave <player>

Args:
<player>       - Player id or name
<arenaId>      - (string) Arena asset id
<lobbyId>      - (string) Lobby instance id
<team>         - (uint) Arena player team index",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (flags != null && flags.Length > 0)
            return "lobby: invalid syntax".MarkErrorCommandOutput();

        if (args == null || args.Length == 0)
            return "lobby: invalid syntax".MarkErrorCommandOutput();

        var sub = args[0].ToLowerInvariant();

        try
        {

            switch (sub)
            {
                case "arenalist":
                    {
                        if (args.Length != 1)
                            return "lobby arenalist: invalid syntax".MarkErrorCommandOutput();

                        var arenas = LobbyManager.arenas.Select(arena => arena.id);
                        var arenaListString = string.Join("\n", arenas);
                        return $"lobby arenalist({arenas.Count()}):\n{arenaListString}".MarkInfoCommandOutput();
                    }

                case "list":
                    {
                        if (args.Length != 1)
                            return "lobby list: invalid syntax".MarkErrorCommandOutput();

                        var lobbies = LobbyManager.lobbies.Select(lobby => $"{lobby.Key} ({lobby.Value.arenaId}) ({lobby.Value.players.Length} players)");
                        var lobbyListString = string.Join("\n", lobbies);
                        return $"lobby list({lobbies.Count()}):\n{lobbyListString}".MarkInfoCommandOutput();
                    }

                case "create":
                    {
                        if (args.Length != 3)
                            return "lobby create: invalid syntax".MarkErrorCommandOutput();

                        string lobbyId = args[1];
                        string arenaId = args[2];

                        if (!LobbyManager.arenas.Any(arena => arena.id == arenaId))
                            return "lobby create: arena not found".MarkErrorCommandOutput();

                        if (LobbyManager.lobbies.ContainsKey(lobbyId))
                            return "lobby create: lobby already exists".MarkErrorCommandOutput();

                        if (LobbyManager.ServerCreateLobby(new() { arenaId = arenaId, lobbyId = lobbyId }))
                            return $"lobby create: created lobby \"{lobbyId}\"".MarkSuccessCommandOutput();
                        else
                            return $"lobby create: failed".MarkErrorCommandOutput();
                    }

                case "info":
                    {
                        if (args.Length != 2)
                            return "lobby info: invalid syntax".MarkErrorCommandOutput();

                        string lobbyId = args[1];

                        if (!LobbyManager.lobbies.TryGetValue(lobbyId, out var lobby))
                            return "lobby info: lobby not found".MarkErrorCommandOutput();

                        var lobbyInfoString = $"lobbyId: {lobbyId}\n";
                        lobbyInfoString += $"arenaId: {lobby.arenaId}\n";
                        lobbyInfoString += $"state: {LobbyManager.ServerGetStringLobbyInstanceState(lobbyId)}\n";
                        lobbyInfoString += $"teamsCount: {lobby.groups.Length}\n";
                        lobbyInfoString += $"players({lobby.players.Length}):\n";
                        lobbyInfoString += string.Join("\n", lobby.players.Select(lobbyPlayer =>
                        {
                            if (NetworkPlayerList.Singleton.Players.TryGetValue(lobbyPlayer.connId, out var player))
                                return $"p{lobbyPlayer.connId} (team: {lobbyPlayer.groupId}) (name: {player.name})";
                            else
                                return $"p{lobbyPlayer.connId} (team: {lobbyPlayer.groupId})";
                        }));

                        return $"lobby info:\n{lobbyInfoString}".MarkInfoCommandOutput();
                    }

                case "start":
                    {
                        if (args.Length != 2)
                            return "lobby start: invalid syntax".MarkErrorCommandOutput();

                        string lobbyId = args[1];

                        if (LobbyManager.ServerStartLobby(lobbyId))
                            return $"lobby start: started lobby \"{lobbyId}\"".MarkSuccessCommandOutput();
                        else
                            return $"lobby start: lobby not found".MarkErrorCommandOutput();
                    }

                case "restart":
                    {
                        if (args.Length != 2)
                            return "lobby restart: invalid syntax".MarkErrorCommandOutput();

                        string lobbyId = args[1];

                        if (LobbyManager.ServerRestartLobby(lobbyId))
                            return $"lobby restart: restarted \"{lobbyId}\"".MarkSuccessCommandOutput();
                        else
                            return $"lobby restart: lobby not found".MarkErrorCommandOutput();
                    }

                case "stop":
                    {
                        if (args.Length != 2)
                            return "lobby stop: invalid syntax".MarkErrorCommandOutput();

                        string lobbyId = args[1];

                        if (LobbyManager.ServerStopLobby(lobbyId))
                            return $"lobby stop: stopped lobby \"{lobbyId}\"".MarkSuccessCommandOutput();
                        else
                            return $"lobby stop: lobby not found".MarkErrorCommandOutput();
                    }

                case "delete":
                    {
                        if (args.Length != 2)
                            return "lobby delete: invalid syntax".MarkErrorCommandOutput();

                        string lobbyId = args[1];

                        if (LobbyManager.ServerDeleteLobby(lobbyId))
                            return $"lobby delete: deleted lobby \"{lobbyId}\"".MarkSuccessCommandOutput();
                        else
                            return $"lobby delete: lobby not found".MarkErrorCommandOutput();
                    }

                case "reset":
                    {
                        if (args.Length != 2)
                            return "lobby reset: invalid syntax".MarkErrorCommandOutput();

                        string lobbyId = args[1];


                        if (LobbyManager.ServerRestartLobby(lobbyId))
                            return $"lobby reset: reset lobby \"{lobbyId}\"".MarkSuccessCommandOutput();
                        else
                            return $"lobby reset: lobby not found".MarkErrorCommandOutput();
                    }

                case "join":
                    {
                        if (args.Length != 4)
                            return "lobby join: invalid syntax".MarkErrorCommandOutput();

                        string lobbyId = args[1];
                        string player = args[2];

                        if (!uint.TryParse(args[3], out uint team))
                            return "lobby join: <team> must be uint".MarkInfoCommandOutput();

                        if (!CommandUtils.TryGetPlayerFromArgument(player, out var playerConn))
                            return "lobby join: <player> not found".MarkErrorCommandOutput();

                        if (!LobbyManager.lobbies.TryGetValue(lobbyId, out var lobby))
                            return "lobby join: lobby not found".MarkErrorCommandOutput();

                        if (lobby.groups.Length <= team)
                            return "lobby join: invalid <team>".MarkErrorCommandOutput();

                        if (LobbyManager.ServerJoinLobby(playerConn, new() { lobbyId = lobbyId, group = (int)team }))
                            return $"lobby join: p{playerConn.connectionId} successfully joined {lobbyId} (team={team})".MarkSuccessCommandOutput();
                        else
                            return $"lobby join: failed".MarkErrorCommandOutput();
                    }

                case "leave":
                    {
                        if (args.Length != 2)
                            return "lobby leave: invalid syntax".MarkErrorCommandOutput();

                        string player = args[1];

                        if (!CommandUtils.TryGetPlayerFromArgument(player, out var playerConn))
                            return "lobby leave: <player> not found".MarkErrorCommandOutput();

                        LobbyManager.ServerLeaveLobby(playerConn);

                        return $"lobby leave: removed player {playerConn.connectionId} from all lobbies".MarkSuccessCommandOutput();
                    }

                default:
                    return "lobby: unknown subcommand".MarkErrorCommandOutput();
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("create lobby cmd error: " + e);
            return "lobby: error".MarkErrorCommandOutput();
        }
    }
}