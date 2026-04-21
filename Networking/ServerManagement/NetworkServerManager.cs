// Author: František Holubec
// Created: 14.07.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.WordGenerating;
using FishNet;
using FishNet.Transporting;
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

        public IEnumerable<AServerRecord> ServerList => _servers.Values;
        public Signal ServerListUpdated { get; } = new();
        public ServerConfig ServerConfig => _ServerConfig;
        
        private readonly Dictionary<long, AServerRecord> _servers = new();
        
        public AServerRecord CurrentServer { get; set; }

        private bool _serverRunning;
        
        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            await AppCore.Services.AwaitRegistered<MasterNetworkManager>();
            if (_ServerConfig.ServerID == 0)
                _ServerConfig.ServerID = GenerateServerID();
            
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
            var masterNetworkManager = await AppCore.Services.AwaitRegistered<MasterNetworkManager>();
            masterNetworkManager.ServerPrepareHandlers += OnServerPrepareHandlers;
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
        }
        
        public void EnumerateAdapters(Action<AServerListAdapter> action)
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
            foreach (var adapter in _Adapters)
            { 
                if (adapter ==null)
                    continue;
                await adapter.PrepareServerStart();
            }
        }
        
        private void OnServerConnectionStateChanged(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started && InstanceFinder.ServerManager.IsOnlyOneServerStarted())
            {
                CurrentServer = new EmptyServerRecord
                {
                    ServerID = _ServerConfig.ServerID,
                    ServerName = _ServerConfig.ServerName,
                    MaxPlayers = _ServerConfig.MaxPlayers,
                    CurrentPlayers = InstanceFinder.ServerManager.Clients.Count
                };
                EnumerateAdapters(adapter => adapter.StartServer());
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped && !InstanceFinder.ServerManager.IsAnyServerStarted())
            {
                _serverRunning = false;
                EnumerateAdapters(adapter => adapter.StopServer());
                CurrentServer = null;
            }
        }

        private void OnClientConnectionStateChanged(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                CurrentServer = null;
            }
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
                foreach (var (id, server) in adapter.Servers)
                {
                    _servers.TryAdd(id, server);
                }
            });
            ServerListUpdated.Dispatch();
        }
        
        private static long GenerateServerID()
        {
            var value1 = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            var value2 = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            return value1 + ((long) value2 << 32);
        }
    }
}
