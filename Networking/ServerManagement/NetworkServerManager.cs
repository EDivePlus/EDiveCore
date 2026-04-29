// Author: František Holubec
// Created: 14.07.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Configuration;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.Networking.Utils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.WordGenerating;
using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
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

        public IEnumerable<ServerRecord> ServerList => _servers.Values;
        public Signal ServerListUpdated { get; } = new();
        public ServerConfig ServerConfig => _ServerConfig;

        private readonly Dictionary<string, ServerRecord> _servers = new();
        
        public ServerRecord HostServer { get; private set; }
        public ServerRecord JoinedServer { get; private set; }
        public ServerRecord CurrentServer =>  HostServer ?? JoinedServer;

        private bool _serverRunning;
        private MasterNetworkManager _masterNetworkManager;

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            _masterNetworkManager = await AppCore.Services.AwaitRegistered<MasterNetworkManager>();
            
            _ServerConfig.ServerID ??= Guid.NewGuid().ToString();
            _ServerConfig.InstanceID ??= Guid.NewGuid().ToString();
            await AppCore.Services.Get<LocalConfigLoader>().SaveConfig(_ServerConfig);

            if (string.IsNullOrWhiteSpace(_ServerConfig.ServerName))
                _ServerConfig.ServerName = _ServerNameGenerator.Generate();

            foreach (var adapter in _Adapters)
            {
                if (adapter ==null)
                    continue;
                await adapter.Initialize(_ServerConfig);
            }
            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionStateChanged;
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionStateChanged;
            _masterNetworkManager.ServerPrepareHandlers += OnServerPrepareHandlers;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionStateChanged;
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionStateChanged;
            if (_masterNetworkManager != null)                
                _masterNetworkManager.ServerPrepareHandlers -= OnServerPrepareHandlers;
        }
        
        public void ConnectToServer(ServerRecord server, AServerEndpoint endpoint = null)
        {
            ConnectToServerAsync(server, endpoint).Forget();
        }
        
        public async UniTask ConnectToServerAsync(ServerRecord server, AServerEndpoint endpoint = null)
        {
            JoinedServer = server;
            bool success;
            if (endpoint != null)
            {
                // Connect using the specified endpoint
                success = await endpoint.PrepareForConnect();
            }
            else
            {
                // No specific endpoint provided, try all endpoints until one succeeds
                success = await server.PrepareForConnect();
            }
            
            if (!success)
            {
                JoinedServer = null;
                return;
            }

            _masterNetworkManager.StartRuntime(NetworkRuntimeMode.Client);
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
            var tugboat = InstanceFinder.TransportManager.GetTransport<Tugboat>();
            if (tugboat == null)
                return;

            if (_ServerConfig.Port > 0)
            {
                tugboat.SetPort(_ServerConfig.Port);
                Debug.Log($"[NetworkServerManager] Using configured port {_ServerConfig.Port}");
            }
            else
            {
                var port = NetworkUtils.FindFreeUdpPort();
                tugboat.SetPort(port);
                Debug.Log($"[NetworkServerManager] Using dynamic port {port}");
            }
        }

        private void OnServerConnectionStateChanged(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started && InstanceFinder.ServerManager.IsOnlyOneServerStarted())
            {
                HostServer = new ServerRecord
                {
                    InstanceID = _ServerConfig.InstanceID,
                    ServerName = _ServerConfig.ServerName,
                    MaxPlayers = _ServerConfig.MaxPlayers,
                    CurrentPlayers = InstanceFinder.ServerManager.Clients.Count,
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
            else if (args.ConnectionState == LocalConnectionState.Stopped && !InstanceFinder.ServerManager.IsAnyServerStarted())
            {
                _serverRunning = false;
                EnumerateAdapters(adapter => adapter.StopServer());
                HostServer = null;
            }
        }

        private void OnClientConnectionStateChanged(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                JoinedServer = null;
            }

            if (JoinedServer != null)
                JoinedServer.CurrentPlayers = InstanceFinder.ServerManager.Clients.Count;
        }

        public void StartSearch()
        {
            if (_serverRunning)
                return;

            _servers.Clear();
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
            ServerListUpdated.Dispatch();
        }
    }
}
