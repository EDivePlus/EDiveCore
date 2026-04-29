// Author: František Holubec
// Created: 08.08.2025

using System.Net;
using FishNet;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.LocalNetwork
{
    public class NetworkDiscovery : ANetworkDiscovery<NetworkDiscoveryResponse>
    {
        [SerializeField]
        private ServerConfig _Config;
        
        protected override NetworkDiscoveryResponse ProcessRequest(IPEndPoint endpoint)
        {
            var tugboat = InstanceFinder.TransportManager.GetTransport<Tugboat>();
            return new NetworkDiscoveryResponse
            {
                InstanceID = _Config.InstanceID,
                ServerName = _Config.ServerName,
                Port = tugboat != null ? tugboat.GetPort() : (ushort) 0,
                MaxPlayers = _Config.MaxPlayers,
                CurrentPlayers = InstanceFinder.ServerManager.Clients.Count,
            };
        }
    }
}
