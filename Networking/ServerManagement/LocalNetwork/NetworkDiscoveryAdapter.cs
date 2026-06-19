// Author: František Holubec
// Created: 15.07.2025

#if NETWORK_DISCOVERY
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Core.Versions;
using EDIVE.Networking.Utils;
using PurrNet;
using PurrNet.Transports;
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

        public override void StartSearch() { }
        public override void StopSearch() { }

        private IEnumerable<AServerEndpoint> GetLocalServerEndpoints()
        {
            var nm = NetworkManager.main;
            var address = NetworkUtils.GetLocalIPv4();
            var port = nm != null && nm.TryGetCurrentTransport<UDPTransport>(out var udp) ? udp.serverPort : (ushort) 0;
            if (string.IsNullOrEmpty(address) || port == 0)
                yield break;

            yield return new AddressServerEndpoint
            {
                Name = "Local Direct",
                Address = address,
                Port = port,
            };
        }

        public override void RegisterHostServer(ServerRecord hostServer)
        {
            base.RegisterHostServer(hostServer);
            hostServer.Endpoints.AddRange(GetLocalServerEndpoints());
        }

        public override UniTask<(bool, ServerRecord)> TryHandleJoinRequest(IJoinRequest request) => UniTask.FromResult((false, default(ServerRecord)));

        private static ServerRecord GetRecord(IPEndPoint endPoint, NetworkDiscoveryResponse response)
        {
            if (AppVersion.FromBaseString(response.Version) != AppCore.CurrentVersion)
                return null;
            
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
                JoinCode = response.JoinCode,
                Version = AppVersion.FromBaseString(response.Version),
            };
        }
    }
}
#endif
