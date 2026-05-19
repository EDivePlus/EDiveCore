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
                ThrowTest(),
                Fps()
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
        
        private static ConsoleCommand ThrowTest() => new("throw-test", "throw debug exception",
            _ => UniTask.Void(() => throw new Exception("Debug Exception")));

        private static ConsoleCommand Fps() => new("fps", "measure main thread FPS (avg over N frames, default 20)",
            async args =>
            {
                var frames = 20;
                if (args.Length > 0 && (!int.TryParse(args[0], out frames) || frames < 1))
                {
                    ConsoleCommandHandler.AppendLog("[red]Usage:[/] fps [[frames]]  (frames must be a positive integer)");
                    return;
                }

                await UniTask.SwitchToMainThread();

                var totalDelta = 0f;
                var minDelta = float.MaxValue;
                var maxDelta = 0f;
                for (var i = 0; i < frames; i++)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    var dt = Time.unscaledDeltaTime;
                    totalDelta += dt;
                    if (dt < minDelta) minDelta = dt;
                    if (dt > maxDelta) maxDelta = dt;
                }

                var avgDelta = totalDelta / frames;
                var avgFps = 1f / avgDelta;
                var maxFps = 1f / minDelta;
                var minFps = 1f / maxDelta;
                ConsoleCommandHandler.AppendLog(
                    $"[green]FPS[/] avg [yellow]{avgFps:F1}[/] ({avgDelta * 1000f:F2} ms) " +
                    $"min [yellow]{minFps:F1}[/] max [yellow]{maxFps:F1}[/] over {frames} frames");
            });
    }
}
#endif
