// Author: František Holubec
// Created: 22.03.2025

using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.Core.Restart;
using EDIVE.Core.Services;
using EDIVE.External.Signals;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace EDIVE.Networking
{
    public class MasterNetworkManager : MonoBehaviour, IService, ILoadable
    {
        private NetworkManager _networkManager;

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
        public Signal<ConnectionState> ConnectionStateChanged { get; } = new();
        
        public NetworkRuntimeMode RuntimeMode { get; private set; } = NetworkRuntimeMode.Offline;
        public Signal<NetworkRuntimeMode> RuntimeModeChanged { get; } = new();
        
        private LocalConnectionState _serverConnectionState = LocalConnectionState.Stopped;
        private LocalConnectionState _clientConnectionState = LocalConnectionState.Stopped;

        private bool _serverStartRequested;
        private bool _clientStartRequested;
        
        public Signal BeforeHostStarted { get; } = new();
        public Signal BeforeServerStarted { get; } = new();
        public Signal BeforeClientStarted { get; } = new();
        
        public event Func<UniTask> ServerPrepareHandlers;
        
        public async UniTask Load(Action<float> progressCallback)
        {
            // Wait one frame for FishNet to initialize
            await UniTask.Yield();
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null)
            {
                Debug.LogError("NetworkManager is not initialized. Make sure FishNet is set up correctly.");
                return;
            }
            
            var multiPass = InstanceFinder.TransportManager.GetTransport<Multipass>();
            if (multiPass != null) multiPass.SetClientTransport<Tugboat>();

            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionStateChanged;
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionStateChanged;

            AppCore.Services.Register(this);
        }

        private void OnServerConnectionStateChanged(ServerConnectionStateArgs args)
        {
            _serverConnectionState = args.ConnectionState;
            RefreshRuntimeMode();
            RefreshConnectionState();
        }

        private void OnClientConnectionStateChanged(ClientConnectionStateArgs args)
        {
            _clientConnectionState = args.ConnectionState;
            RefreshRuntimeMode();
            RefreshConnectionState();
        }

        private void RefreshRuntimeMode()
        {
            NetworkRuntimeMode newMode;
            var isServer = _serverConnectionState is LocalConnectionState.Started or LocalConnectionState.Starting;
            var isClient = _clientConnectionState is LocalConnectionState.Started or LocalConnectionState.Starting;
            if (isServer && isClient)
            {
                newMode = NetworkRuntimeMode.Host;
            }
            else if (isServer)
            {
                newMode = NetworkRuntimeMode.Server;
            }
            else if (isClient)
            {
                newMode = NetworkRuntimeMode.Client;
            }
            else
            {
                newMode = NetworkRuntimeMode.Offline;
            }
            
            if (newMode != RuntimeMode)
            {
                RuntimeMode = newMode;
                RuntimeModeChanged.Dispatch(newMode);
            }
        }

        private void RefreshConnectionState()
        {
            var newState = ResolveConnectionState();
            if (newState != ConnectionState)
            {
                ConnectionState = newState;
                ConnectionStateChanged.Dispatch(newState);
            }
        }

        private ConnectionState ResolveConnectionState()
        {
            if (_clientConnectionState == LocalConnectionState.Stopped)
            {
                return _serverConnectionState switch
                {
                    LocalConnectionState.Stopped => ConnectionState.Disconnected,
                    LocalConnectionState.Stopping => ConnectionState.Disconnecting,
                    LocalConnectionState.Starting => ConnectionState.Connecting,
                    LocalConnectionState.Started => ConnectionState.Connected,
                    _ => throw new ArgumentOutOfRangeException()
                };
            } 
            
            return _clientConnectionState switch
            {
                LocalConnectionState.Stopped => ConnectionState.Disconnected,
                LocalConnectionState.Stopping => ConnectionState.Disconnecting,
                LocalConnectionState.Starting => ConnectionState.Connecting,
                LocalConnectionState.Started => ConnectionState.Connected,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        public void OnDestroy()
        {
            AppCore.Services.Unregister(this);
        }

        public void StartHost()
        {
            if (!CanStartServer() || !CanStartClient())
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartHost: runtime is already {RuntimeMode}.");
                return;
            }
            BeforeHostStarted?.Dispatch();
            StartServer();
            StartClient();
        }

        // The server can be started directly from the ServerManager or Transport
        public void StartServer()
        {
            if (!CanStartServer())
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartServer: server is {_serverConnectionState}.");
                return;
            }
            _serverStartRequested = true;
            StartServerAsync().Forget();
        }

        private async UniTaskVoid StartServerAsync()
        {
            try
            {
                if (ServerPrepareHandlers != null)
                {
                    var tasks = ServerPrepareHandlers.GetInvocationList()
                        .Cast<Func<UniTask>>()
                        .Where(h => h != null)
                        .Select(h => h());

                    await UniTask.WhenAll(tasks);
                }

                BeforeServerStarted?.Dispatch();
                InstanceFinder.ServerManager.StartConnection();
            }
            finally
            {
                _serverStartRequested = false;
            }
        }

        // The client can be started directly from the ClientManager or Transport
        public void StartClient()
        {
            if (!CanStartClient())
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartClient: client is {_clientConnectionState}.");
                return;
            }
            _clientStartRequested = true;
            try
            {
                BeforeClientStarted?.Dispatch();
                InstanceFinder.ClientManager.StartConnection();
            }
            finally
            {
                _clientStartRequested = false;
            }
        }

        private bool CanStartServer() => !_serverStartRequested && _serverConnectionState == LocalConnectionState.Stopped;
        private bool CanStartClient() => !_clientStartRequested && _clientConnectionState == LocalConnectionState.Stopped;
        
        public void SetAddress(string text)
        {
            InstanceFinder.TransportManager.Transport.SetClientAddress(text);
        }
        
        public void SetPort(ushort port)
        {
            InstanceFinder.TransportManager.Transport.SetPort(port);
        }
        
        public ushort GetPort()
        {
            return InstanceFinder.TransportManager.Transport.GetPort();
        }
        
        public void StartRuntime(NetworkRuntimeMode runtimeMode)
        {
            if (runtimeMode == NetworkRuntimeMode.Offline)
                return;

            if (RuntimeMode != NetworkRuntimeMode.Offline || _serverStartRequested || _clientStartRequested)
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartRuntime({runtimeMode}): runtime is already {RuntimeMode}.");
                return;
            }

            switch (runtimeMode)
            {
                case NetworkRuntimeMode.Client:
                    StartClient();
                    break;
                case NetworkRuntimeMode.Server:
                    StartServer();
                    break;
                case NetworkRuntimeMode.Host:
                    StartHost();
                    break;
            }
        }

        public void StopRuntime()
        {
            _serverStartRequested = false;
            _clientStartRequested = false;
            InstanceFinder.ServerManager.StopConnection(true);
            InstanceFinder.ClientManager.StopConnection();
        }
        
        [ExecuteOnAppRestart(-90)]
        public static UniTask OnAppRestart()
        {
            if (!AppCore.Services.TryGet<MasterNetworkManager>(out var networkManager))
            {
                Debug.LogError("Cannot stop runtime, missing network manager");
                return UniTask.CompletedTask;
            }
            
            networkManager.StopRuntime();
            return UniTask.CompletedTask;
        }
    }
}
