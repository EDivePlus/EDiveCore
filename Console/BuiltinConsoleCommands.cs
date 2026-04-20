using Spectre.Console;
using UnityEngine;

namespace EDIVE.Utils
{
    public static class BuiltinConsoleCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SpectreBootstrap.RegisterCommands(
                Quit(),
                Help(),
                Status(),
                Clear()
            );
        }

        private static ConsoleCommand Quit() => new("quit", "shutdown server",
            async _ =>
            {
                var result = false;
                SpectreBootstrap.Exclusive(() => result = AnsiConsole.Confirm("[red]Really shutdown?[/]"));
                if (result)
                {
                    SpectreBootstrap.AppendLog("[red]Shutting down...[/]");
                    await SpectreBootstrap.OnMainThread(Application.Quit);
                }
                else
                {
                    SpectreBootstrap.AppendLog("[grey]Shutdown cancelled.[/]");
                }
            });

        private static ConsoleCommand Help() => new("help", "show this message",
            _ =>
            {
                SpectreBootstrap.AppendLog("[cyan]Available commands:[/]");
                foreach (var c in SpectreBootstrap.GetCommands())
                    SpectreBootstrap.AppendLog($"  [yellow]{Markup.Escape(c.Name),-18}[/] - {Markup.Escape(c.Description)}");
            });

        private static ConsoleCommand Status() => new("status", "server status",
            async _ =>
            {
                await SpectreBootstrap.OnMainThread(() =>
                {
                    SpectreBootstrap.AppendLog($"[green]Server running[/] - uptime: {Time.realtimeSinceStartup:F1}s");
                });
            });

        private static ConsoleCommand Clear() => new("clear", "clear the terminal",
            _ => SpectreBootstrap.ClearScreen());
    }
}
