// Author: František Holubec
// Created: 14.07.2025

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Configuration;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.Networking.Utils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.WordGenerating;
using PurrNet;
using PurrNet.Transports;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public class NetworkServerManager : ALoadableServiceBehaviour<NetworkServerManager>
    {
       
        [ShowCreateNew]
        [SerializeField]
        private ServerConfig _ServerConfig;
        
        [ShowCreateNew]
        [SerializeField]
        private AWordGenerator _ServerNameGenerator;

        [SerializeField]
        [InfoBox("Adapters are ordered by their priority. Higher priority adapters will be used first.")]
        private List<AServerListAdapter> _Adapters = new();

        [SerializeField]
        [Tooltip("How long to wait for a client connection attempt to reach Started state before declaring the endpoint failed and trying the next one.")]
        private float _ConnectAttemptTimeoutSeconds = 10f;
        
        public IEnumerable<ServerRecord> ServerList => _serverList;
        public Signal ServerListUpdated { get; } = new();
        public ServerConfig ServerConfig => _ServerConfig;
        
        private readonly Dictionary<string, ServerRecord> _servers = new();

        [HideReferenceObjectPicker]
        [ShowInInspector]   
        [EnableGUI]
        private readonly List<ServerRecord> _serverList = new();
        
        public ServerRecord HostServer { get; private set; }
        public ServerRecord JoinedServer { get; private set; }
        public ServerRecord CurrentServer =>  HostServer ?? JoinedServer;

        private bool _serverRunning;
        private bool _connecting;
        private MasterNetworkManager _masterNetworkManager;

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            _masterNetworkManager = await AppCore.Services.AwaitRegistered<MasterNetworkManager>();
            
            if (string.IsNullOrEmpty(_ServerConfig.ServerID))
                _ServerConfig.ServerID = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(_ServerConfig.InstanceID))
                _ServerConfig.InstanceID = Guid.NewGuid().ToString();
            
            await AppCore.Services.Get<LocalConfigLoader>().SaveConfig(_ServerConfig);

            if (string.IsNullOrWhiteSpace(_ServerConfig.ServerName))
                _ServerConfig.ServerName = _ServerNameGenerator.Generate();

            foreach (var adapter in _Adapters)
            {
                if (adapter ==null)
                    continue;
                await adapter.Initialize(_ServerConfig);
            }

            var nm = NetworkManager.main;
            nm.onServerConnectionState += OnServerConnectionStateChanged;
            nm.onClientConnectionState += OnClientConnectionStateChanged;
            nm.onPlayerJoined += OnPlayerJoined;
            nm.onPlayerLeft += OnPlayerLeft;
            _masterNetworkManager.RegisterServerPrepareHandler(OnServerPrepareHandlers);
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
            EnumerateAdapters(adapter => adapter.PopulateDependencies(dependencies));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopServer();
            
            if (NetworkManager.main != null)
            {
                NetworkManager.main.onServerConnectionState -= OnServerConnectionStateChanged;
                NetworkManager.main.onClientConnectionState -= OnClientConnectionStateChanged;
                NetworkManager.main.onPlayerJoined -= OnPlayerJoined;
                NetworkManager.main.onPlayerLeft -= OnPlayerLeft;
            }
            if (_masterNetworkManager != null)
                _masterNetworkManager.UnregisterServerPrepareHandler(OnServerPrepareHandlers);
        }
        
        public void ConnectToServer(ServerRecord server, AServerEndpoint endpoint = null)
        {
            ConnectToServerAsync(server, endpoint).Forget();
        }
        
        public async UniTask ConnectToServerAsync(ServerRecord server, AServerEndpoint endpoint = null, CancellationToken cancellationToken = default)
        {
            if (server == null)
                return;

            if (_connecting)
            {
                Debug.LogWarning("[NetworkServerManager] Connect attempt already in progress, ignoring.");
                return;
            }

            var endpoints = endpoint != null
                ? new List<AServerEndpoint> { endpoint }
                : server.Endpoints;

            if (endpoints == null || endpoints.Count == 0)
            {
                Debug.LogWarning($"[NetworkServerManager] Server '{server.ServerName}' has no endpoints to try.");
                return;
            }

            JoinedServer = server;
            _connecting = true;
            try
            {
                for (var i = 0; i < endpoints.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var ep = endpoints[i];
                    if (ep == null)
                        continue;

                    Debug.Log($"[NetworkServerManager] Trying endpoint {i + 1}/{endpoints.Count}: {ep}");
                    if (await TryConnectAsync(ep, cancellationToken))
                        return;

                    Debug.LogWarning($"[NetworkServerManager] Endpoint failed: {ep}");
                }

                Debug.LogError($"[NetworkServerManager] All {endpoints.Count} endpoint(s) failed for server '{server.ServerName}'.");
                JoinedServer = null;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[NetworkServerManager] Connect attempt to '{server.ServerName}' canceled.");
                NetworkManager.main.StopClient();
                JoinedServer = null;
                throw;
            }
            finally
            {
                _connecting = false;
            }
        }
        
        private async UniTask<bool> TryConnectAsync(AServerEndpoint endpoint, CancellationToken cancellationToken)
        {
            if (!await endpoint.PrepareForConnect())
                return false;

            var attempt = new UniTaskCompletionSource<bool>();
            var stopped = new UniTaskCompletionSource();
            void OnState(ConnectionState state)
            {
                switch (state)
                {
                    case ConnectionState.Connecting:
                        break;
                    case ConnectionState.Connected:
                        attempt.TrySetResult(true);
                        break;
                    case ConnectionState.Disconnecting:
                        attempt.TrySetResult(false);
                        break;
                    case ConnectionState.Disconnected:
                        attempt.TrySetResult(false);
                        stopped.TrySetResult();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var nm = NetworkManager.main;
            nm.onClientConnectionState += OnState;

            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(Mathf.Max(1f, _ConnectAttemptTimeoutSeconds)));
            await using var registration = attemptCts.Token.Register(() => attempt.TrySetResult(false));

            try
            {
                _masterNetworkManager.StartClient();
                var success = await attempt.Task;

                // Successful connect AND the caller still wants it → done.
                if (success && !cancellationToken.IsCancellationRequested)
                    return true;

                nm.StopClient();
                await stopped.Task.AttachExternalCancellation(cancellationToken).SuppressCancellationThrow();

                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }
            finally
            {
                nm.onClientConnectionState -= OnState;
            }
        }
        
        private void EnumerateAdapters(Action<AServerListAdapter> action)
        {
            foreach (var adapter in _Adapters)
            {
                if (adapter ==null)
                    continue;
                action(adapter);
            }
        }

        private async UniTask OnServerPrepareHandlers()
        {
            _serverRunning = true;
            ResolveServerPort();
            foreach (var adapter in _Adapters)
            {
                if (adapter ==null)
                    continue;
                await adapter.PrepareServerStart();
            }
        }

        private void ResolveServerPort()
        {       
            var nm = NetworkManager.main;
            if (!nm.TryGetCurrentTransport(out UDPTransport udp))
                return;

            var port = _ServerConfig.Port;
            if (port <= 0)
            {
                port = NetworkUtils.FindFreeUdpPort();
                Debug.Log($"[NetworkServerManager] Using dynamic port {port}");
            }
            else
            {
                Debug.Log($"[NetworkServerManager] Using configured port {port}");
            }
            udp.serverPort = port;
            _ServerConfig.ResolvedPort = port;
        }

        private void OnServerConnectionStateChanged(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                HostServer = new ServerRecord
                {
                    InstanceID = _ServerConfig.InstanceID,
                    ServerName = _ServerConfig.ServerName,
                    MaxPlayers = _ServerConfig.MaxPlayers,
                    CurrentPlayers = NetworkManager.main.playerCount,
                    LastUpdated = DateTime.UtcNow,
                };

                EnumerateAdapters(adapter =>
                {
                    var endpoints = adapter.GetLocalServerEndpoints();
                    if (endpoints == null)
                        return;
                    foreach (var endpoint in endpoints)
                    {
                        if (endpoint != null)
                            HostServer.Endpoints.Add(endpoint);
                    }
                });

                EnumerateAdapters(adapter => adapter.StartServer());
            }
            else if (state == ConnectionState.Disconnected)
            {
                StopServer();
            }
        }

        private void StopServer()
        {
            _serverRunning = false;
            EnumerateAdapters(adapter => adapter.StopServer());
            HostServer = null;
        }

        private void OnClientConnectionStateChanged(ConnectionState state)
        {
            // During a multi-endpoint connect attempt, transient Disconnected events fire between
            // failed endpoints. Only clear JoinedServer for "real" disconnects — once the
            // failover loop is no longer in progress.
            if (state == ConnectionState.Disconnected && !_connecting)
            {
                JoinedServer = null;
            }

            if (JoinedServer != null)
                JoinedServer.CurrentPlayers = NetworkManager.main.playerCount;
        }

        private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
        {
            if (!asServer) return;
            RefreshHostPlayerCount();
        }

        private void OnPlayerLeft(PlayerID player, bool asServer)
        {
            if (!asServer) return;
            RefreshHostPlayerCount();
        }

        private void RefreshHostPlayerCount()
        {
            if (HostServer == null)
                return;

            HostServer.CurrentPlayers = NetworkManager.main.playerCount;
            HostServer.LastUpdated = DateTime.UtcNow;
        }

        public void StartSearch()
        {
            if (_serverRunning)
                return;

            _servers.Clear();
            _serverList.Clear();
            EnumerateAdapters(adapter =>
            {
                adapter.ServerListUpdated.RemoveListener(OnAdapterServerListUpdated);
                adapter.ServerListUpdated.AddListener(OnAdapterServerListUpdated);
                adapter.StartSearch();
            });
        }

        public void StopSearch()
        {
            EnumerateAdapters(adapter =>
            {
                adapter.ServerListUpdated.RemoveListener(OnAdapterServerListUpdated);
                adapter.StopSearch();
            });
        }

        private void OnAdapterServerListUpdated()
        {
            _serverList.Clear();
            _servers.Clear();
            EnumerateAdapters(adapter =>
            {
                foreach (var contribution in adapter.Servers.Values)
                {
                    if (!_servers.TryGetValue(contribution.InstanceID, out var record))
                    {
                        record = new ServerRecord(contribution.InstanceID);
                        _servers[contribution.InstanceID] = record;
                    }

                    if (contribution.Endpoints != null)
                        record.Endpoints.AddRange(contribution.Endpoints);

                    if (contribution.LastUpdated > record.LastUpdated)
                    {
                        record.ServerName = contribution.ServerName;
                        record.MaxPlayers = contribution.MaxPlayers;
                        record.CurrentPlayers = contribution.CurrentPlayers;
                        record.LastUpdated = contribution.LastUpdated;
                    }
                }
            });
            _serverList.AddRange(_servers.Values);
            ServerListUpdated.Dispatch();
        }
    }
}
