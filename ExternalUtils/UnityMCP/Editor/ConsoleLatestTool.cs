// Author: František Holubec
// Created: 30.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace EDIVE.UnityMCP.Editor
{
    [McpForUnityTool("console_latest", Description = "Returns the newest Unity console entries, newest last. Filter by type and text.")]
    public static class ConsoleLatestTool
    {
        private const BindingFlags STATIC = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags INSTANCE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const int ERROR_MASK = (1 << 0) | (1 << 1) | (1 << 4) | (1 << 6) | (1 << 8) | (1 << 11) | (1 << 13) | (1 << 17) | (1 << 21) | (1 << 22);
        private const int WARNING_MASK = (1 << 7) | (1 << 9) | (1 << 12);
        private static readonly int[] LOG_LEVEL_FLAGS = {1 << 7, 1 << 8, 1 << 9};

        public class Parameters
        {
            [ToolParameter("How many entries to return.", Required = false, DefaultValue = "20")]
            public int Count { get; set; }

            [ToolParameter("Comma separated types: error, warning, log. Default all.", Required = false)]
            public string Types { get; set; }

            [ToolParameter("Only entries containing this text.", Required = false)]
            public string Contains { get; set; }

            [ToolParameter("Include the stack trace lines.", Required = false, DefaultValue = "false")]
            public bool IncludeStack { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            var count = Math.Max(1, ToolParams.GetInt(@params, "Count", 20));
            var types = (ToolParams.GetString(@params, "Types") ?? "error,warning,log").Split(',').Select(t => t.Trim().ToLowerInvariant()).ToHashSet();
            var contains = ToolParams.GetString(@params, "Contains");
            var includeStack = ToolParams.GetBool(@params, "IncludeStack", false);

            var logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            var logEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
            if (logEntriesType == null || logEntryType == null)
                return new ErrorResponse("UnityEditor.LogEntries is not available in this Unity version.");

            var messageField = logEntryType.GetField("message", INSTANCE);
            var modeField = logEntryType.GetField("mode", INSTANCE);
            var getEntry = logEntriesType.GetMethod("GetEntryInternal", STATIC);
            var entry = Activator.CreateInstance(logEntryType);

            // Entries follow the Console window's level toggles, show all levels while reading
            var flagsProperty = logEntriesType.GetProperty("consoleFlags", STATIC);
            var setFlag = logEntriesType.GetMethod("SetConsoleFlag", STATIC);
            var flags = (int) flagsProperty!.GetValue(null);
            foreach (var bit in LOG_LEVEL_FLAGS)
                setFlag!.Invoke(null, new object[] {bit, true});

            var result = new List<object>();
            var total = (int) logEntriesType.GetMethod("StartGettingEntries", STATIC)!.Invoke(null, null);
            try
            {
                for (var i = total - 1; i >= 0 && result.Count < count; i--)
                {
                    getEntry!.Invoke(null, new[] {i, entry});
                    var mode = (int) modeField!.GetValue(entry);
                    var type = (mode & ERROR_MASK) != 0 ? "error" : (mode & WARNING_MASK) != 0 ? "warning" : "log";
                    if (!types.Contains(type))
                        continue;

                    var message = (string) messageField!.GetValue(entry) ?? "";
                    if (!string.IsNullOrEmpty(contains) && message.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    if (!includeStack)
                        message = message.Split('\n')[0];
                    result.Add(new {index = i, type, message});
                }
            }
            finally
            {
                logEntriesType.GetMethod("EndGettingEntries", STATIC)!.Invoke(null, null);
                foreach (var bit in LOG_LEVEL_FLAGS)
                    setFlag!.Invoke(null, new object[] {bit, (flags & bit) != 0});
            }

            result.Reverse();
            return new SuccessResponse($"{result.Count} of {total} entries", new {total, entries = result});
        }
    }
}
