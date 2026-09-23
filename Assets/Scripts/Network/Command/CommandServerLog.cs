using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public static class CommandServerLog
{
    private static readonly string[] allowedFlags = new string[] { "e", "w", "l", "a" };

    public static CommandObject commandObject = new()
    {
        hideFromDebugLog = true,
        name = "serverlog",
        shortHelp = "serverlog [length|state] - Print server logs",
        fullHelp = @"
serverlog - Print logs from current server session

Syntax:
serverlog <length> <flags>
serverlog state

Args:
<length> - Optional max number of latest log entries
state    - Show server log statistics

Flags:
-e   - Errors / Exceptions
-w   - Warnings
-l   - Logs
-a   - All logs (default)",
        execute = Execute
    };

    private static string Execute(string[] args, string[] flags)
    {
        if (args.Length > 1 || (flags.Length > 0 && !flags.IsFits(allowedFlags)))
            return "serverlog: invalid syntax".MarkErrorCommandOutput();

        List<LogCollector.LogEntry> logs = LogCollector.GetLogsSnapshot();

        if (args.Length == 1 && args[0].Equals("state", StringComparison.OrdinalIgnoreCase))
        {
            if (flags.Length > 0)
                return "serverlog: flags cannot be used with 'state'".MarkErrorCommandOutput();

            int logsCount = logs.Count(l => l.type == LogType.Log);
            int warningsCount = logs.Count(l => l.type == LogType.Warning);
            int errorsCount = logs.Count(l => l.type == LogType.Error);
            int exceptionsCount = logs.Count(l => l.type == LogType.Exception);

            var uptime = DateTime.UtcNow - LogCollector.ServerStartTime;

            var result = new StringBuilder();
            result.AppendLine("serverlog state:");
            result.AppendLine($"uptime: {uptime:dd\\.hh\\:mm\\:ss}");
            result.AppendLine($"logs: {logsCount}");
            result.AppendLine($"warnings: {warningsCount}");
            result.AppendLine($"errors: {errorsCount}");
            result.AppendLine($"exceptions: {exceptionsCount}");
            result.AppendLine($"total: {logs.Count}");

            return result.ToString().MarkInfoCommandOutput();
        }

        int length = -1;

        if (args.Length == 1 && !int.TryParse(args[0], out length))
            return $"serverlog: invalid argument: {args[0]}".MarkErrorCommandOutput();

        bool filterError = flags.Contains("e");
        bool filterWarning = flags.Contains("w");
        bool filterLog = flags.Contains("l");
        bool filterAll = flags.Length == 0 || flags.Contains("a");

        IEnumerable<LogCollector.LogEntry> filtered = logs;

        if (!filterAll)
        {
            filtered = filtered.Where(l =>
                (filterError && (l.type == LogType.Error || l.type == LogType.Exception)) ||
                (filterWarning && l.type == LogType.Warning) ||
                (filterLog && l.type == LogType.Log)
            );
        }

        if (length > 0)
            filtered = filtered.TakeLast(length);

        var sb = new StringBuilder();

        sb.Append("serverlog");

        if (length > 0)
            sb.Append($" ({length})");

        if (flags.Length > 0)
            sb.Append($" ({string.Join(", ", flags)})");

        sb.AppendLine(":");

        int count = 0;

        foreach (var log in filtered)
        {
            sb.Append($"[{log.time:HH:mm:ss}] ");
            sb.Append($"[{log.type}] ");
            sb.AppendLine(log.message);
            count++;
        }

        sb.AppendLine($"total: {count}");

        return sb.ToString().MarkInfoCommandOutput();
    }
}