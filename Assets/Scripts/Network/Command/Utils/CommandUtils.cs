using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using UnityEngine;

public static class CommandUtils
{
    private const string playerTankArgumentPattern = "^p-?[0-9]+$";

    public static string MarkErrorCommandOutput(this string text)
    {
        return "#ERROR#" + text;
    }

    public static string MarkSuccessCommandOutput(this string text)
    {
        return "#SUCCESS#" + text;
    }

    public static string MarkInfoCommandOutput(this string text)
    {
        return "#INFO#" + text;
    }

    public static readonly Dictionary<TankControlManager.TankController, string> tankControllerNames = new()
    {
        { TankControlManager.TankController.Player, "player" },
        { TankControlManager.TankController.AI, "ai" },
        { TankControlManager.TankController.None, "none" }
    };

    public static bool TryGetPlayerTankId(int connectionId, out uint tankId)
    {
        tankId = 0;
        if (NetworkServer.connections.TryGetValue(connectionId, out var conn) && conn.identity)
            tankId = conn.identity.netId;
        return tankId > 0;
    }

    public static bool TryGetTankType(NetworkTank tank, out string result)
    {
        result = "";
        var tankAssetId = tank.netIdentity.assetId;
        foreach (var originalTank in GameTanksManager.Singleton.tanks)
            if (tankAssetId == originalTank.PrefabAssetId)
            {
                result = originalTank.id;
                return true;
            }
        return false;
    }

    public static string GetControllerName(NetworkTank tank)
    {
        if (tankControllerNames.TryGetValue(tank.ControlManager.serverCurrentController, out var name))
            return name;
        else
            return "none";
    }

    public static bool TryGetPlayerFromArgument(string playerArgument, out NetworkConnectionToClient conn)
    {
        conn = null;
        if (playerArgument.TryParseNoLocale(out int plrId) && NetworkServer.connections.TryGetValue(plrId, out conn))
            return true;
        else if (Regex.IsMatch(playerArgument, playerTankArgumentPattern) && playerArgument[1..].TryParseNoLocale(out plrId) && NetworkServer.connections.TryGetValue(plrId, out conn))
            return true;
        else
        {
            try
            {
                plrId = NetworkPlayerList.Singleton.Players.Values.First(plr => plr.name == playerArgument).id;
                return NetworkServer.connections.TryGetValue(plrId, out conn);
            }
            catch
            {
                return false;
            }
        }
    }

    public static bool TryGetTankFromArgument(string tankArgument, out NetworkTank tank, bool requireAlive = true)
    {
        tank = null;
        if (tankArgument.TryParseNoLocale(out uint netId))
            tank = GameTanksManager.Singleton.ServerGetTankInstances().FirstOrDefault(tank => tank.netId == netId);
        else if (Regex.IsMatch(tankArgument, playerTankArgumentPattern) && tankArgument[1..].TryParseNoLocale(out int plrId))
            tank = GameTanksManager.Singleton.ServerGetTankInstances()
                .FirstOrDefault(tank => tank.connectionToClient != null && tank.connectionToClient.connectionId == plrId);
        else
        {
            try
            {
                plrId = NetworkPlayerList.Singleton.Players.Values.First(plr => plr.name == tankArgument).id;
                if (NetworkServer.connections.TryGetValue(plrId, out var conn) && conn.identity)
                    conn.identity.TryGetComponent(out tank);
            }
            catch
            {
                return false;
            }
        }
        return tank != null && tank != default && tank && (!requireAlive || !tank.Death.IsDead);
    }

    public static bool TryGetPositionFromArgument(string positionArgument, out Vector2 position)
    {
        position = Vector2.zero;
        var elements = positionArgument.Split(',');
        if (elements.Length == 2)
        {
            if (elements[0].TryParseNoLocale(out float x) && elements[1].TryParseNoLocale(out float y))
            {
                position = new(x, y);
                return true;
            }
        }
        return false;
    }

    public static bool TryGetDestinationFromArgument(string destinationArgument, out Vector2 destination)
    {
        destination = Vector2.zero;
        if (TryGetTankFromArgument(destinationArgument, out var destinationTank))
            destination = destinationTank.transform.position;
        else if (TryGetPositionFromArgument(destinationArgument, out var destinationPosition))
            destination = destinationPosition;
        else
            return false;
        return true;
    }
}