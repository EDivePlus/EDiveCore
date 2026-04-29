// Author: František Holubec
// Created: 21.04.2026

#if SPECTRE_CONSOLE
using EDIVE.Console;
using EDIVE.Core;
using EDIVE.Networking.ServerManagement;
using FishNet;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    public static class ConsoleUtils
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            ConsoleCommandHandler.RegisterCommands(
                NetStatus()
            );
        }

        private static ConsoleCommand NetStatus() => new("NetStatus", "Status of network",
            async _ =>
            {
                await ConsoleCommandHandler.OnMainThread(() =>
                {
                    if (!AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
                    {
                        ConsoleCommandHandler.AppendLog("[red]NetworkServerManager not registered[/]");
                        return;
                    }

                    var serverRunning = InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.IsAnyServerStarted();
                    var serverName = serverManager.CurrentServer?.ServerName ?? "<unknown>";
                    var maxPlayers = serverManager.CurrentServer?.MaxPlayers ?? 0;
                    var playerCount = serverManager.CurrentServer?.CurrentPlayers ?? 0;

                    ConsoleCommandHandler.AppendLog(serverRunning ? "[green]Server running[/]" : "[red]Server stopped[/]");
                    ConsoleCommandHandler.AppendLog($"Server name: [yellow]{serverName}[/]");
                    ConsoleCommandHandler.AppendLog($"Players connected: [cyan]{playerCount}[/]/[cyan]{maxPlayers}[/]");

                    var endpoints = serverManager.CurrentServer?.Endpoints;
                    if (endpoints != null)
                    {
                        foreach (var endpoint in endpoints)
                            ConsoleCommandHandler.AppendLog($"Endpoint: [cyan]{endpoint}[/]");
                    }
                });
            });
    }
}
#endif
