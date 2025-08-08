// Author: František Holubec
// Created: 08.08.2025

using System.Net;
using FishNet;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.LocalNetwork
{
    public class NetworkDiscovery : ANetworkDiscovery<NetworkDiscoveryResponse>
    {
        [SerializeField]
        private ServerConfig _Config;
        
        protected override NetworkDiscoveryResponse ProcessRequest(IPEndPoint endpoint)
        {
            return new NetworkDiscoveryResponse
            {
                ServerID = _Config.ServerID,
                ServerName = _Config.ServerName,
                MaxPlayers = _Config.MaxPlayers,
                CurrentPlayers = InstanceFinder.ServerManager.Clients.Count,
            };
        }
    }
}
