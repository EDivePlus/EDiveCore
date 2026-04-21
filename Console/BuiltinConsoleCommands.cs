#if SPECTRE_CONSOLE
using System;
using Cysharp.Threading.Tasks;
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
                Quit(),
                Help(),
                Status(),
                Clear(),
                Throw()
            );
        }

        private static ConsoleCommand Quit() => new("quit", "shutdown server",
            async _ =>
            {
                var result = false;
                ConsoleCommandHandler.Exclusive(() => result = AnsiConsole.Confirm("[red]Really shutdown?[/]"));
                if (result)
                {
                    ConsoleCommandHandler.AppendLog("[red]Shutting down...[/]");
                    await ConsoleCommandHandler.OnMainThread(Application.Quit);
                }
                else
                {
                    ConsoleCommandHandler.AppendLog("[grey]Shutdown cancelled.[/]");
                }
            });

        private static ConsoleCommand Help() => new("help", "show this message",
            _ =>
            {
                ConsoleCommandHandler.AppendLog("[cyan]Available commands:[/]");
                foreach (var c in ConsoleCommandHandler.GetCommands())
                    ConsoleCommandHandler.AppendLog($"  [yellow]{Markup.Escape(c.Name),-18}[/] - {Markup.Escape(c.Description)}");
            });

        private static ConsoleCommand Status() => new("status", "server status",
            async _ =>
            {
                await ConsoleCommandHandler.OnMainThread(() =>
                {
                    ConsoleCommandHandler.AppendLog($"[green]Server running[/] - uptime: {Time.realtimeSinceStartup:F1}s");
                });
            });

        private static ConsoleCommand Clear() => new("clear", "clear the terminal",
            _ => ConsoleCommandHandler.ClearScreen());
        
        private static ConsoleCommand Throw() => new("throw", "throw debug exception",
            _ => UniTask.Void(() => throw new Exception("Debug Exception")));
    }
}
#endif
