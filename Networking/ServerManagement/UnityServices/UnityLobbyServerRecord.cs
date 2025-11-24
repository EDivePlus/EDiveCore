// Author: František Holubec
// Created: 21.11.2025

using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Networking.Utils;
using FishNet;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting.UTP;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UnityServices
{
    public class UnityLobbyServerRecord : AServerRecord
    {
        public Lobby Lobby;
        public string RelayJoinCode;
        public string DirectConnectAddress;
        
        public override async UniTask<bool> PrepareForConnect()
        {
            var transportManager = InstanceFinder.TransportManager;

            var multiPass = transportManager.GetTransport<Multipass>();
            if (await IsServerReachable(DirectConnectAddress, 7766))
            {
                if (multiPass != null) 
                    multiPass.SetClientTransport<Tugboat>();
                
                var tugboat = transportManager.GetTransport<Tugboat>();
                if (tugboat != null)
                {
                    tugboat.SetClientAddress(DirectConnectAddress);
                    Debug.Log($"[ServerRecord] Connect using direct address {DirectConnectAddress}");
                    return true;
                }
            }
            
            var unityTransport = transportManager.GetTransport<UnityTransport>();
            if (unityTransport == null)
                return false;

            try
            {
                var allocation = await RelayService.Instance.JoinAllocationAsync(RelayJoinCode);
                unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            
            Debug.Log($"[ServerRecord] Connect using relay code {RelayJoinCode}");
            return true;
        }
        
        public static async UniTask<bool> IsServerReachable(string ip, int port, int timeoutMs = 1000)
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ip, port);

            if (await UniTask.WhenAny(connectTask.AsUniTask(), UniTask.Delay(timeoutMs)) == 0)
                return true;

            return false;
        }
    }
}
