// Author: František Holubec
// Created: 22.03.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.Core.Restart;
using EDIVE.External.Signals;
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
        public Signal<ConnectionState> ConnectionStateChanged { get; } = new();

        public NetworkRuntimeMode RuntimeMode { get; private set; } = NetworkRuntimeMode.Offline;
        public Signal<NetworkRuntimeMode> RuntimeModeChanged { get; } = new();

        private ConnectionState _serverConnectionState = ConnectionState.Disconnected;
        private ConnectionState _clientConnectionState = ConnectionState.Disconnected;

        private bool _serverStartRequested;
        private bool _clientStartRequested;

        public Signal BeforeHostStarted { get; } = new();
        public Signal BeforeServerStarted { get; } = new();
        public Signal BeforeClientStarted { get; } = new();
        
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
            return _clientConnectionState == ConnectionState.Disconnected
                ? _serverConnectionState
                : _clientConnectionState;
        }
        

        public void StartHost()
        {
            if (!CanStartServer() || !CanStartClient())
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartHost: runtime is already {RuntimeMode}.");
                return;
            }

            BeforeHostStarted?.Dispatch();
            UniTask.Void(async () =>
            {
                if (AppCore.Services.TryGet<TransportController>(out var transportController)) 
                    transportController.SetHost();
                
                await StartServerInternalAsync();
                StartClientInternal();
            });
        }

        public void StartServer()
        {
            if (!CanStartServer())
            {
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartServer: server is {_serverConnectionState}.");
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

                BeforeServerStarted?.Dispatch();
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
                BeforeClientStarted?.Dispatch();
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
                Debug.LogWarning($"[MasterNetworkManager] Ignoring StartClient: client is {_clientConnectionState}.");
                return;
            }

            StartClientInternal();
        }

        private bool CanStartServer() => !_serverStartRequested && _serverConnectionState == ConnectionState.Disconnected;
        private bool CanStartClient() => !_clientStartRequested && _clientConnectionState == ConnectionState.Disconnected;
        
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
