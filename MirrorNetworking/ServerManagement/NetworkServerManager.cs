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
using UnityEngine;

namespace EDIVE.MirrorNetworking.ServerManagement
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
        private List<AServerListAdapter> _Adapters = new();

        public IEnumerable<ServerRecord> ServerList => _servers.Values;
        public Signal ServerListUpdated { get; } = new();
        public ServerConfig ServerConfig => _ServerConfig;
        
        private readonly Dictionary<long, ServerRecord> _servers = new();
        
        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            _ServerConfig.ServerID = GenerateServerID();
            _ServerConfig.ServerName = _ServerNameGenerator.Generate();

            foreach (var adapter in _Adapters)
            {
                await adapter.Initialize(_ServerConfig);
            }
        }

        public void StartSearch()
        {
            _servers.Clear();
            foreach (var adapter in _Adapters)
            {
                adapter.ServerListUpdated.RemoveListener(OnAdapterServerListUpdated);
                adapter.ServerListUpdated.AddListener(OnAdapterServerListUpdated);
                adapter.StartSearch();
            }
        }
        
        public void StopSearch()
        {
            foreach (var adapter in _Adapters)
            {
                adapter.ServerListUpdated.RemoveListener(OnAdapterServerListUpdated);
                adapter.StopSearch();
            }
        }

        private void OnAdapterServerListUpdated()
        {
            _servers.Clear();
            foreach (var adapter in _Adapters)
            {
                foreach (var (id, server) in adapter.Servers)
                { 
                    _servers[id] = server;
                }
            }
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
