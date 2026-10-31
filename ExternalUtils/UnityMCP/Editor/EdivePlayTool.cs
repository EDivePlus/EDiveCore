// Author: František Holubec
// Created: 30.10.2026

using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Utils;
using EDIVE.Core;
using EDIVE.Networking;
using EDIVE.Networking.ServerManagement;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using PurrNet;
using PurrNet.Transports;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace EDIVE.UnityMCP.Editor
{
    [McpForUnityTool("edive_play",
        Description =
            "EDIVE play mode and networking: play from RootScene, stop, status (loading, network mode), host, offline, join by code or address, stop network. Host and join need the app to have finished loading.")]
    public static class EdivePlayTool
    {
        public class Parameters
        {
            [ToolParameter("status | play | stop | host | offline | join_code | join_address | stop_network")]
            public string Action { get; set; }

            [ToolParameter("Join code for join_code.", Required = false)]
            public string Code { get; set; }

            [ToolParameter("Address for join_address.", Required = false)]
            public string Address { get; set; }

            [ToolParameter("Port for join_address.", Required = false)]
            public int Port { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            var action = ToolParams.GetString(@params, "Action")?.ToLowerInvariant() ?? "status";
            switch (action)
            {
                case "status":
                    return new SuccessResponse("Status", GetStatus());

                case "play":
                    if (EditorApplication.isPlaying)
                        return new SuccessResponse("Already playing", GetStatus());
                    // The save prompt would be a modal dialog that blocks every MCP call
                    if (HasDirtyScenes())
                        return new ErrorResponse("Open scenes have unsaved changes. Save or discard them first.");
                    return LoaderEditorUtility.TryPlayRootScene()
                        ? new SuccessResponse("Entering play mode from RootScene. Poll status until loaded is true.")
                        : new ErrorResponse("Could not start play mode from RootScene (unsaved scene prompt or missing root scene).");

                case "stop":
                    EditorApplication.isPlaying = false;
                    return new SuccessResponse("Stopping play mode");
            }

            if (!EditorApplication.isPlaying || !AppCore.IsLoaded)
                return new ErrorResponse("The app is not loaded yet. Use action=play, then poll status until loaded is true.");

            if (!AppCore.Services.TryGet<MasterNetworkManager>(out var network))
                return new ErrorResponse("MasterNetworkManager is not registered.");

            // PurrNet takes a moment to stop, starts before that are ignored
            if (action != "stop_network" && NetworkManager.main != null &&
                (NetworkManager.main.serverState != ConnectionState.Disconnected || NetworkManager.main.clientState != ConnectionState.Disconnected) &&
                network.RuntimeMode == NetworkRuntimeMode.None)
                return new ErrorResponse("The network is still stopping, retry in a moment.");

            switch (action)
            {
                case "host":
                case "offline":
                    if (network.RuntimeMode != NetworkRuntimeMode.None)
                        return new ErrorResponse($"Network already running as {network.RuntimeMode}.");
                    if (action == "host")
                        network.StartHost();
                    else
                        network.StartOffline();
                    return new SuccessResponse($"Starting {action}. Poll status for the network mode.");

                case "join_code":
                case "join_address":
                    if (network.RuntimeMode != NetworkRuntimeMode.None)
                        return new ErrorResponse($"Network already running as {network.RuntimeMode}.");
                    if (!AppCore.Services.TryGet<NetworkServerManager>(out var servers))
                        return new ErrorResponse("NetworkServerManager is not registered.");

                    IJoinRequest request = action == "join_code"
                        ? new CodeJoinRequest(ToolParams.GetString(@params, "Code"))
                        : new DirectJoinRequest(ToolParams.GetString(@params, "Address"), (ushort) ToolParams.GetInt(@params, "Port", 0));
                    JoinAsync(servers, request).Forget();
                    return new SuccessResponse("Joining. Poll status for the network mode, check console_latest for the join result.");

                case "stop_network":
                    network.StopRuntime();
                    return new SuccessResponse("Stopping network runtime");

                default:
                    return new ErrorResponse($"Unknown action '{action}'.");
            }
        }

        private static async UniTaskVoid JoinAsync(NetworkServerManager servers, IJoinRequest request)
        {
            var error = await servers.ConnectToServerAsync(request);
            UnityEngine.Debug.Log($"[EdivePlayTool] Join finished: {error}");
        }

        private static bool HasDirtyScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    return true;
            }

            return false;
        }

        private static object GetStatus()
        {
            var mode = AppCore.Services.TryGet<MasterNetworkManager>(out var network) ? network.RuntimeMode.ToString() : "unavailable";
            return new
            {
                playing = EditorApplication.isPlaying,
                compiling = EditorApplication.isCompiling,
                loaded = AppCore.IsLoaded,
                network = mode
            };
        }
    }
}
