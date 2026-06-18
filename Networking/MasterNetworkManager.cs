// Author: František Holubec
// Created: 22.03.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.Core.Restart;
using EDIVE.Networking.ServerManagement;
using EDIVE.ServiceHub;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;

namespace EDIVE.Networking
{
    public class MasterNetworkManager : ALoadableServiceBehaviour<MasterNetworkManager>
    {
        [SerializeField]
        private ServerConfig _ServerConfig;
        
        [SerializeField]
        private StatisticsManager _StatisticsManager;

        private NetworkManager _networkManager;

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
        public event Action<ConnectionState> ConnectionStateChanged;

        public NetworkRuntimeMode RuntimeMode { get; private set; } = NetworkRuntimeMode.None;
        public event Action<NetworkRuntimeMode> RuntimeModeChanged;

        private ConnectionState _serverConnectionState = ConnectionState.Disconnected;
        private ConnectionState _clientConnectionState = ConnectionState.Disconnected;

        private bool _serverStartRequested;
        private bool _clientStartRequested;

        public event Action BeforeHostStarted;
        public event Action BeforeServerStarted;
        public event Action BeforeClientStarted;
        
        public StatisticsManager StatisticsManager => _StatisticsManager;

        private struct PriorityPrepareHandler
        {
            public Func<UniTask> Handler;
            public int Priority;
        }
        
        private readonly List<PriorityPrepareHandler> _serverPrepareHandlers = new();

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            await UniTask.Yield();
            _networkManager = NetworkManager.main;
            if (_networkManager == null)
            {
                Debug.LogError("NetworkManager is not initialized. Make sure PurrNet is set up correctly.");
                return;
            }

            _networkManager.onClientConnectionState += OnClientConnectionStateChanged;
            _networkManager.onServerConnectionState += OnServerConnectionStateChanged;

            AppCore.Services.Register(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AppCore.Services.Unregister(this);
        }
        
        public void RegisterServerPrepareHandler(Func<UniTask> handler, int priority = 0)
        {
            if (_serverPrepareHandlers.Any(h => h.Handler == handler))
                return;
            
            _serverPrepareHandlers.Add(new PriorityPrepareHandler { Handler = handler, Priority = priority });
            _serverPrepareHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
        
        public void UnregisterServerPrepareHandler(Func<UniTask> handler)
        {
            _serverPrepareHandlers.RemoveAll(h => h.Handler == handler);
        }
        
        private void OnServerConnectionStateChanged(ConnectionState state)
        {
            _serverConnectionState = state;
            RefreshRuntimeMode();
            RefreshConnectionState();

            if (AppCore.Services.TryGet<ServiceHubManager>(out var serviceHub))
            {
                if (state == ConnectionState.Disconnected)
                {
                    serviceHub.SaveData.FlushAllServerDirtyEntries(destroyCancellationToken).Forget();
                }
            }
        }

        private void OnClientConnectionStateChanged(ConnectionState state)
        {
            _clientConnectionState = state;
            RefreshRuntimeMode();
            RefreshConnectionState();

            if (AppCore.Services.TryGet<ServiceHubManager>(out var serviceHub))
            {
                if (state == ConnectionState.Connected)
                {
                    serviceHub.ClientAuth.OnLoggedOut += StopRuntime;
                }
                if (state == ConnectionState.Disconnected)
                {
                    serviceHub.ClientAuth.OnLoggedOut -= StopRuntime;
                }
            }
        }

        private void RefreshRuntimeMode()
        {
            NetworkRuntimeMode newMode;
            var isServer = _serverConnectionState is ConnectionState.Connected or ConnectionState.Connecting;
            var isClient = _clientConnectionState is ConnectionState.Connected or ConnectionState.Connecting;
            if (isServer && isClient)
            {
                newMode = NetworkManager.main.transport is LocalTransport ? NetworkRuntimeMode.Offline : NetworkRuntimeMode.Host;
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
                newMode = NetworkRuntimeMode.None;
            }

            if (newMode != RuntimeMode)
            {
                RuntimeMode = newMode;
                RuntimeModeChanged?.Invoke(newMode);
            }
        }

        private void RefreshConnectionState()
        {
            var newState = ResolveConnectionState();
            if (newState != ConnectionState)
            {
                ConnectionState = newState;
                ConnectionStateChanged?.Invoke(newState);
            }
        }

        private ConnectionState ResolveConnectionState()
        {
            return _clientConnectionState == ConnectionState.Disconnected
                ? _serverConnectionState
                : _clientConnectionState;
        }

        private void StartHostInternal(bool isOffline)
        {
            if (!CanStartServer() || !CanStartClient())
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartHost: runtime is already {RuntimeMode}.");
                return;
            }

            BeforeHostStarted?.Invoke();
            UniTask.Void(async () =>
            {
                if (AppCore.Services.TryGet<TransportController>(out var transportController))
                {
                    if (isOffline)  
                        transportController.SetLocal();
                    else
                        transportController.SetHost();
                }
                
                await StartServerInternalAsync();
                StartClientInternal();
            });
        }

        public void StartHost()
        {
            StartHostInternal(false);
        }

        public void StartOffline()
        {
            StartHostInternal(true);
        }

        public void StartServer()
        {
            if (!CanStartServer())
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartServer: server is {LiveServerState}.");
                return;
            }
            
            if (AppCore.Services.TryGet<TransportController>(out var transportController)) 
                transportController.SetServer();
            
            _serverStartRequested = true;
            StartServerInternalAsync().Forget();
        }

        private async UniTask StartServerInternalAsync()
        {
            try
            {
                if (AppCore.Services.TryGet<ServiceHubManager>(out var serviceHub))
                {
                    await serviceHub.ServerAuth.PrepareServerAuthAsync(_ServerConfig.ServerID, _ServerConfig.ServerSecret, destroyCancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                if (_serverPrepareHandlers != null)
                {
                    var tasks = _serverPrepareHandlers.Select(h => h.Handler)
                        .Where(h => h != null)
                        .Select(h => h());
                    await UniTask.WhenAll(tasks);
                }

                BeforeServerStarted?.Invoke();
                NetworkManager.main.StartServer();
            }
            finally
            {
                _serverStartRequested = false;
            }
        }

        private void StartClientInternal()
        {
            _clientStartRequested = true;
            try
            {
                BeforeClientStarted?.Invoke();
                NetworkManager.main.StartClient();
            }
            finally
            {
                _clientStartRequested = false;
            }
        }
        
        public void StartClient()
        {
            if (!CanStartClient())
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartClient: client is {LiveClientState}.");
                return;
            }

            StartClientInternal();
        }
        
        private bool CanStartServer() => !_serverStartRequested && LiveServerState == ConnectionState.Disconnected;
        private bool CanStartClient() => !_clientStartRequested && LiveClientState == ConnectionState.Disconnected;

        private static ConnectionState LiveServerState => NetworkManager.main != null ? NetworkManager.main.serverState : ConnectionState.Disconnected;
        private static ConnectionState LiveClientState => NetworkManager.main != null ? NetworkManager.main.clientState : ConnectionState.Disconnected;
        
        public void StartRuntime(NetworkRuntimeMode runtimeMode)
        {
            if (runtimeMode == NetworkRuntimeMode.None)
                return;

            if (RuntimeMode != NetworkRuntimeMode.None || _serverStartRequested || _clientStartRequested)
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
                case NetworkRuntimeMode.Offline:
                    StartOffline();
                    break;
            }
        }

        public void StopRuntime()
        {
            _serverStartRequested = false;
            _clientStartRequested = false;
            var nm = NetworkManager.main;
            if (nm != null)
            {
                nm.StopServer();
                nm.StopClient();
            }
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
