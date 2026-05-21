// Author: František Holubec
// Created: 21.04.2026

#if SPECTRE_CONSOLE
using Cysharp.Threading.Tasks;
using EDIVE.Console;
using EDIVE.Core;
using EDIVE.Networking.ServerManagement;
using PurrNet;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    public static class NetworkConsoleUtils
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            ConsoleCommandHandler.RegisterCommands(
                NetStatus()
            );
            AppCore.WhenLoaded(() =>
            {
                ConsoleCommandHandler.InvokeCommand(NetStatus());
            });
        }

        private static ConsoleCommand NetStatus() => new("NetStatus", "Status of network",
            async _ =>
            {
                await UniTask.SwitchToMainThread();
                if (!AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
                {
                    ConsoleCommandHandler.AppendLog("[red]NetworkServerManager not registered[/]");
                    return;
                }

                var serverRunning = NetworkManager.main != null && NetworkManager.main.isServer;
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
    }
}
#endif
