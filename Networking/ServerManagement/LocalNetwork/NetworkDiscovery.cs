// Author: František Holubec
// Created: 08.08.2025

using System.Net;
using System.Text;
using EDIVE.Networking.Utils;
using Newtonsoft.Json;
using PurrNet;
using PurrNet.Transports;
using UnityNetworkDiscovery.Runtime;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.LocalNetwork
{
    public class NetworkDiscovery : ANetworkDiscovery<NetworkDiscoveryResponse>
    {
        [SerializeField]
        private ServerConfig _Config;

        protected override NetworkDiscoveryResponse CreateResponse(IPEndPoint endpoint)
        {
            var nm = NetworkManager.main;
            var port = nm != null && nm.TryGetCurrentTransport<UDPTransport>(out var udp) ? udp.serverPort : (ushort) 0;
            return new NetworkDiscoveryResponse
            {
                InstanceID = _Config.InstanceID,
                ServerName = _Config.ServerName,
                Port = port,
                MaxPlayers = _Config.MaxPlayers,
                CurrentPlayers = nm != null ? nm.playerCount : 0,
            };
        }

        protected override byte[] SerializeResponse(NetworkDiscoveryResponse response)
        {
            var json = JsonConvert.SerializeObject(response);
            return Encoding.UTF8.GetBytes(json);
        }

        protected override bool TryDeserializeResponse(byte[] data, out NetworkDiscoveryResponse response)
        {
            try
            {
                var json = Encoding.UTF8.GetString(data);
                response = JsonConvert.DeserializeObject<NetworkDiscoveryResponse>(json);
                return true;
            }
            catch
            {
                response = null;
                return false;
            }
        }
    }
}
