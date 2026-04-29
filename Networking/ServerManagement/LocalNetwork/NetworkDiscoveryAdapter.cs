// Author: František Holubec
// Created: 15.07.2025

using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using EDIVE.Networking.Utils;
using FishNet;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.LocalNetwork
{
    public class NetworkDiscoveryAdapter : AServerListAdapter
    {
        [SerializeField]
        private NetworkDiscovery _NetworkDiscovery;

        public override UniTask Initialize()
        {
            _NetworkDiscovery.ServerListUpdated += OnServerListUpdated;
            return UniTask.CompletedTask;
        }

        private void OnServerListUpdated()
        {
            SetServers(_NetworkDiscovery.ServerList.Select(s => GetRecord(s.endPoint, s.response)));
        }

        public override void StartSearch()
        {

        }

        public override void StopSearch()
        {

        }

        public override IEnumerable<AServerEndpoint> GetLocalServerEndpoints()
        {
            var tugboat = InstanceFinder.TransportManager.GetTransport<Tugboat>();
            var address = NetworkUtils.GetLocalIPv4();
            var port = tugboat != null ? tugboat.GetPort() : (ushort) 0;
            if (string.IsNullOrEmpty(address) || port == 0)
                yield break;

            yield return new AddressServerEndpoint
            {
                Name = "Local Direct",
                Address = address,
                Port = port,
            };
        }

        private static ServerRecord GetRecord(IPEndPoint endPoint, NetworkDiscoveryResponse response)
        {
            var endpoints = new List<AServerEndpoint>();
            var address = endPoint.Address.ToString();
            if (!string.IsNullOrEmpty(address) && response.Port > 0)
            {
                endpoints.Add(new AddressServerEndpoint
                {
                    Name = "Local Direct",
                    Address = address,
                    Port = response.Port,
                });
            }

            return new ServerRecord
            {
                InstanceID = response.InstanceID,
                ServerName = response.ServerName,
                MaxPlayers = response.MaxPlayers,
                CurrentPlayers = response.CurrentPlayers,
                Endpoints = endpoints,
            };
        }
    }
}
