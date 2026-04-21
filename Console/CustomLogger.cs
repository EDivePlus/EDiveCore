using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
using EDIVE.NativeUtils;

#if SPECTRE_CONSOLE
using Spectre.Console;
#endif

namespace EDIVE.Console
{
    public static class CustomLogger
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var logPath = GetArgValue("-customLogPath");
            if (PlatformUtils.IsHeadless() && string.IsNullOrWhiteSpace(logPath))
                logPath = "server.log";
            Debug.unityLogger.logHandler = new CustomLogHandler(Debug.unityLogger.logHandler, logPath);
        }

        private static string GetArgValue(string argName)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == argName)
                    return args[i + 1];
            }
            return null;
        }
    }

    public class CustomLogHandler : ILogHandler
    {
        private readonly ILogHandler _default;
        private readonly StreamWriter _logFile;

        public CustomLogHandler(ILogHandler defaultHandler, string logPath)
        {
            _default = defaultHandler;

            if (!string.IsNullOrWhiteSpace(logPath) && logPath != "-")
                _logFile = new StreamWriter(logPath, append: true) {AutoFlush = true};

            Application.quitting += Shutdown;
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            var timestamp = GetTimeStamp();
            var message = string.Format(format, args);
            _logFile?.WriteLine($"{timestamp} {message}");

#if SPECTRE_CONSOLE
            if (PlatformUtils.IsHeadless())
                ConsoleCommandHandler.AppendLog(FormatForConsole(logType, message));
            else
                _default.LogFormat(logType, context, $"{timestamp} {format}", args);
#else
            _default.LogFormat(logType, context, $"{timestamp} {format}", args);
#endif
        }

        public void LogException(Exception exception, Object context)
        {
            _logFile?.WriteLine($"{GetTimeStamp()} {exception}");
#if SPECTRE_CONSOLE
            if (PlatformUtils.IsHeadless())
                ConsoleCommandHandler.AppendRenderable(exception.GetRenderable());
            else 
                _default.LogException(exception, context);
#else
            _default.LogException(exception, context);
#endif
        }

        private static string GetTimeStamp() => $"{DateTime.UtcNow:yyyy.MM.dd HH:mm:ss}";

        private void Shutdown()
        {
            _logFile?.Close();
        }

#if SPECTRE_CONSOLE
        private static string FormatForConsole(LogType logType, string message)
        {
            var escaped = Markup.Escape(message);
            return logType switch
            {
                LogType.Error or LogType.Assert => $"[red]{escaped}[/]",
                LogType.Warning => $"[yellow]{escaped}[/]",
                _ => escaped,
            };
        }
#endif
    }
}