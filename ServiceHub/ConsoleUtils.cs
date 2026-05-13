// Author: František Holubec
// Created: 21.04.2026

#if SPECTRE_CONSOLE
using EDIVE.Console;
using EDIVE.Core;
using UnityEngine;

namespace EDIVE.ServiceHub
{
    public static class ConsoleUtils
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            ConsoleCommandHandler.RegisterCommands(
                Probe()
            );
        }

        private static ConsoleCommand Probe() => new("Probe", "Probe for open ports",
            async args =>
            {
                if (!AppCore.Services.TryGet<ServiceHubManager>(out var serviceHub))
                {
                    ConsoleCommandHandler.AppendLog("[red]ServiceHubManager not registered[/]");
                    return;
                }
                
                if (args.Length == 0 || !int.TryParse(args[0], out var port))
                {
                    ConsoleCommandHandler.AppendLog("[red]Usage: Probe <port>[/]");
                    return;
                }
                
                var result = await serviceHub.Probe.CheckPortAsync(port);
                
                if (result.Reachable)
                    ConsoleCommandHandler.AppendLog($"[green]Port {port} is open at {result.PublicAddress}:{port}[/]");
                else
                    ConsoleCommandHandler.AppendLog($"[red]Port {port} is closed[/]");
            });
    }
}
#endif
