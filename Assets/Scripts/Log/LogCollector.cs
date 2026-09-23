using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LogCollector : MonoBehaviour
{
    public struct LogEntry
    {
        public string message;
        public string stack;
        public LogType type;
        public DateTime time;
    }

    public static LogCollector Instance { get; private set; }

    [SerializeField]
    private int maxLogs = 5000;

    private static readonly List<LogEntry> logs = new();
    private static readonly object lockObj = new();

    public static DateTime ServerStartTime { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        ServerStartTime = DateTime.UtcNow;

        Application.logMessageReceived -= OnLogReceived;
        Application.logMessageReceived += OnLogReceived;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Application.logMessageReceived -= OnLogReceived;
            Instance = null;
        }
    }

    private void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        lock (lockObj)
        {
            if (logs.Count >= maxLogs)
                logs.RemoveAt(0);

            logs.Add(new LogEntry
            {
                message = condition,
                stack = type == LogType.Error || type == LogType.Exception ? stackTrace : null,
                type = type,
                time = DateTime.UtcNow
            });
        }
    }

    public static List<LogEntry> GetLogsSnapshot()
    {
        lock (lockObj)
        {
            return logs.ToList();
        }
    }

    public static int Count(LogType type)
    {
        lock (lockObj)
        {
            return logs.Count(l => l.type == type);
        }
    }

    public static int TotalCount()
    {
        lock (lockObj)
        {
            return logs.Count;
        }
    }
}