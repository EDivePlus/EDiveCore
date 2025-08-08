// Author: František Holubec
// Created: 15.07.2025

using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.LocalNetwork
{
    public class NetworkDiscoveryAdapter : AServerListAdapter
    {
        [SerializeField]
        private NetworkDiscovery _NetworkDiscovery;

        public override UniTask Initialize(ServerConfig serverConfig)
        {
            _NetworkDiscovery.ServerListUpdated += OnServerListUpdated;
            return UniTask.CompletedTask;
        }

        private void OnServerListUpdated()
        {
            Servers.Clear();
            AddServers(_NetworkDiscovery.ServerList.Select(s => GetRecord(s.endPoint, s.response)));
        }

        private void OnServerListUpdated(IPEndPoint endPoint, NetworkDiscoveryResponse response)
        {
            AddServer(GetRecord(endPoint, response));
        }

        public override void StartSearch()
        {
            Servers.Clear();
            /*
            if (_NetworkDiscovery.IsSearching)
            {
                _NetworkDiscovery.StopSearchingOrAdvertising();
                _NetworkDiscovery.SearchForServers();
            }
            */
        }

        public override void StopSearch()
        {
            
        }

        private static ServerRecord GetRecord(IPEndPoint endPoint, NetworkDiscoveryResponse response)
        {
            return new ServerRecord()
            {
                Address = endPoint.Address.ToString(),
                ServerID = response.ServerID,
                ServerName = response.ServerName,
                MaxPlayers = response.MaxPlayers,
                CurrentPlayers = response.CurrentPlayers
            };
        }
    }
}
