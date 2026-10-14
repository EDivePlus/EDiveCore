// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using EDIVE.External.DomainReloadHelper;
using UnityEngine;

namespace EDIVE.Debugging
{
    public enum DebugLogLevel
    {
        Log,
        Warning,
        Error
    }

    public sealed class DebugLogEntry
    {
        public DebugLogLevel Level { get; }
        public LogType Type { get; }
        public string Message { get; }
        public string StackTrace { get; }
        public DateTime Time { get; }

        // First line of the message, shown in the list
        public string Title { get; }

        // First stack frame, shown under the message
        public string Source { get; }

        public DebugLogEntry(LogType type, string message, string stackTrace, DateTime time)
        {
            Type = type;
            Level = type switch
            {
                LogType.Warning => DebugLogLevel.Warning,
                LogType.Log => DebugLogLevel.Log,
                _ => DebugLogLevel.Error
            };
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            Time = time;
            Title = FirstLine(Message);
            Source = FirstCallerFrame(StackTrace);
        }

        private static string FirstLine(string text)
        {
            var end = text.IndexOf('\n');
            return end < 0 ? text : text.Substring(0, end).TrimEnd('\r');
        }

        // Skips the frames of Unity's own logging calls
        private static string FirstCallerFrame(string stackTrace)
        {
            foreach (var line in stackTrace.Split('\n'))
            {
                var frame = line.Trim();
                if (frame.Length == 0 || frame.StartsWith("UnityEngine.Debug") || frame.StartsWith("UnityEngine.Logger"))
                    continue;
                return frame;
            }
            return string.Empty;
        }
    }

    // Captures Unity logs from any thread, the runner moves them to the main thread each frame.
    public static class DebugLogBuffer
    {
        public const int DEFAULT_CAPACITY = 2000;

        private static readonly object LOCK = new();

        // Cleared in Initialize on every play start
        private static readonly List<DebugLogEntry> PENDING = new();
        private static readonly List<DebugLogEntry> ENTRIES = new();
        private static readonly int[] COUNTS = new int[3];

        [ClearOnReload(Value = DEFAULT_CAPACITY)]
        private static int _capacity = DEFAULT_CAPACITY;

        // Main thread, after new entries or a clear.
        [ClearOnReload]
        public static event Action Changed;

        public static IReadOnlyList<DebugLogEntry> Entries => ENTRIES;

        public static int Count(DebugLogLevel level) => COUNTS[(int) level];

        internal static void Initialize(int capacity)
        {
            _capacity = Mathf.Max(100, capacity);
            lock (LOCK)
            {
                PENDING.Clear();
            }
            ENTRIES.Clear();
            Array.Clear(COUNTS, 0, COUNTS.Length);

            Application.logMessageReceivedThreaded -= OnLogReceived;
            Application.logMessageReceivedThreaded += OnLogReceived;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        public static void Clear()
        {
            ENTRIES.Clear();
            Array.Clear(COUNTS, 0, COUNTS.Length);
            Changed?.Invoke();
        }

        internal static void Flush()
        {
            lock (LOCK)
            {
                if (PENDING.Count == 0)
                    return;

                foreach (var entry in PENDING)
                {
                    ENTRIES.Add(entry);
                    COUNTS[(int) entry.Level]++;
                }
                PENDING.Clear();
            }

            var overflow = ENTRIES.Count - _capacity;
            if (overflow > 0)
            {
                for (var i = 0; i < overflow; i++)
                {
                    COUNTS[(int) ENTRIES[i].Level]--;
                }
                ENTRIES.RemoveRange(0, overflow);
            }

            Changed?.Invoke();
        }

        private static void OnLogReceived(string message, string stackTrace, LogType type)
        {
            var entry = new DebugLogEntry(type, message, stackTrace, DateTime.Now);
            lock (LOCK)
            {
                PENDING.Add(entry);
            }
        }

        private static void OnQuitting() => Application.logMessageReceivedThreaded -= OnLogReceived;
    }
}
