// Author: František Holubec
// Created: 20.04.2026

using System;
using Spectre.Console;
using UnityEngine;

namespace EDIVE.Utils
{
    public class CapturingLogHandler : ILogHandler
    {
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            try
            {
                var msg = args is { Length: > 0 } ? string.Format(format, args) : format;
                OnUnityLog(msg, "", logType);
            }
            catch
            {
                try { OnUnityLog(format, "", logType); }
                catch { }
            }
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            try { OnUnityLog(exception.Message, exception.StackTrace ?? "", LogType.Exception); }
            catch { }
        }
            
        private static void OnUnityLog(string msg, string stack, LogType type)
        {
            try
            {
                var color = type switch
                {
                    LogType.Error or LogType.Exception => "red",
                    LogType.Warning => "yellow",
                    LogType.Assert => "magenta",
                    _ => "grey"
                };
                SpectreBootstrap.AppendLog($"[{color}][[{type}]][/] {Markup.Escape(msg ?? "")}");
            }
            catch { }
        }
    }
}
