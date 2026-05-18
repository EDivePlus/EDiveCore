#if SPECTRE_CONSOLE
using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core.Restart;
using Spectre.Console;
using UnityEngine;

namespace EDIVE.Console
{
    public static class BuiltinConsoleCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            ConsoleCommandHandler.RegisterCommands(
                Help(),
                Quit(),
                Restart(),
                Status(),
                Clear(),
                Throw()
            );
        }

        private static ConsoleCommand Help() => new("help", "show this message",
            _ =>
            {
                ConsoleCommandHandler.AppendLog("[cyan]Available commands:[/]");
                foreach (var c in ConsoleCommandHandler.GetCommands())
                    ConsoleCommandHandler.AppendLog($"  [yellow]{Markup.Escape(c.Name),-18}[/] - {Markup.Escape(c.Description)}");
            });
        
        private static ConsoleCommand Quit() => new("quit", "shutdown app",
            async _ =>
            {
                var result = false;
                ConsoleCommandHandler.Exclusive(() => result = AnsiConsole.Confirm("[red]Really shutdown?[/]"));
                if (result)
                {
                    ConsoleCommandHandler.AppendLog("[red]Shutting down...[/]");
                    await UniTask.SwitchToMainThread();
                    Application.Quit();
                }
                else
                {
                    ConsoleCommandHandler.AppendLog("[grey]Shutdown cancelled.[/]");
                }
            });
        
        private static ConsoleCommand Restart() => new("restart", "restart app",
            async _ =>
            {
                var result = false;
                ConsoleCommandHandler.Exclusive(() => result = AnsiConsole.Confirm("[red]Really restart?[/]"));
                if (result)
                {
                    ConsoleCommandHandler.AppendLog("[red]Restarting...[/]");
                    await UniTask.SwitchToMainThread();
                    AppRestartUtility.RestartAsync().Forget();
                }
                else
                {
                    ConsoleCommandHandler.AppendLog("[grey]Restart cancelled.[/]");
                }
            });
      
        private static ConsoleCommand Status() => new("status", "app status",
            async _ =>
            {
                await UniTask.SwitchToMainThread();
                ConsoleCommandHandler.AppendLog($"[green]App running[/] - uptime: {Time.realtimeSinceStartup:F1}s");
            });

        private static ConsoleCommand Clear() => new("clear", "clear the terminal",
            _ => ConsoleCommandHandler.ClearScreen());
        
        private static ConsoleCommand Throw() => new("throw", "throw debug exception",
            _ => UniTask.Void(() => throw new Exception("Debug Exception")));
    }
}
#endif
