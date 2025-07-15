// Author: František Holubec
// Created: 14.07.2025

using System;
using System.Net;
using EDIVE.External.Signals;
using Mirror;
using Mirror.Discovery;
using UnityEngine;

namespace EDIVE.MirrorNetworking.ServerManagement.LocalNetwork
{
    [DisallowMultipleComponent]
    public class LocalServerDiscovery : NetworkDiscoveryBase<DiscoverServerRequest, DiscoverServerResponse>
    {
        [SerializeField]
        private ServerConfig _Config;
        
        public Signal<DiscoverServerResponse> ServerFound { get; } = new();
        
        protected override DiscoverServerResponse ProcessRequest(DiscoverServerRequest request, IPEndPoint endpoint)
        {
            try
            {
                return new DiscoverServerResponse
                {
                    ServerID = _Config.ServerID,
                    Address = transport.ServerUri(),
                    ServerName = _Config.ServerName,
                    MaxPlayers = _Config.MaxPlayers,
                    CurrentPlayers = NetworkServer.connections.Count,
                };
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }
        
        protected override DiscoverServerRequest GetRequest() => new();
        
        protected override void ProcessResponse(DiscoverServerResponse response, IPEndPoint endpoint)
        {
            var realUri = new UriBuilder(response.Address)
            {
                Host = endpoint.Address.ToString()
            };
            response.Address = realUri.Uri;
            ServerFound.Dispatch(response);
        }
    }
}
