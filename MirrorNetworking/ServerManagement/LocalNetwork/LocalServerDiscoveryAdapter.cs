// Author: František Holubec
// Created: 15.07.2025

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using UnityEngine;

namespace EDIVE.MirrorNetworking.ServerManagement.LocalNetwork
{
    public class LocalServerDiscoveryAdapter : AServerListAdapter
    {
        [SerializeField]
        private LocalServerDiscovery _LocalServerDiscovery;

        public override async UniTask Initialize(ServerConfig serverConfig)
        {
            var networkManager = await AppCore.Services.AwaitRegistered<MasterNetworkManager>();
            networkManager.ServerStarted.AddListener(OnServerStarted);
            
            _LocalServerDiscovery.ServerFound.AddListener(OnServerFound);
        }
        
        private void OnServerStarted()
        {
            UniTask.Void(async () =>
            {
                await UniTask.Yield();
                _LocalServerDiscovery.AdvertiseServer();
            });
        }

        public override void StartSearch()
        {
            Servers.Clear();
            _LocalServerDiscovery.StartDiscovery();
        }

        public override void StopSearch()
        {
            _LocalServerDiscovery.StopDiscovery();
        }
        
        private void OnServerFound(DiscoverServerResponse response)
        {
            AddServer(GetRecord(response));
        }

        private static ServerRecord GetRecord(DiscoverServerResponse discoverServerResponse)
        {
            return new ServerRecord()
            {
                Address = discoverServerResponse.Address.Host,
                ServerID = discoverServerResponse.ServerID,
                ServerName = discoverServerResponse.ServerName,
                MaxPlayers = discoverServerResponse.MaxPlayers,
                CurrentPlayers = discoverServerResponse.CurrentPlayers
            };
        }
    }
}
