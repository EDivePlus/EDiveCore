// Author: František Holubec
// Created: 21.11.2025

using System;
using Cysharp.Threading.Tasks;
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
    public class UnityLobbyServerRecord : ADirectServerRecord
    {
        public Lobby Lobby;
        public string RelayJoinCode;

        public override async UniTask<bool> PrepareForConnect()
        {
            var transportManager = InstanceFinder.TransportManager;
            var multiPass = transportManager.GetTransport<Multipass>();

            if (!string.IsNullOrEmpty(DirectAddress) && DirectPort > 0 && await NetworkUtils.IsServerReachable(DirectAddress, DirectPort))
            {
                if (multiPass != null)
                    multiPass.SetClientTransport<Tugboat>();

                var tugboat = transportManager.GetTransport<Tugboat>();
                if (tugboat != null)
                {
                    tugboat.SetClientAddress(DirectAddress);
                    tugboat.SetPort(DirectPort);
                    Debug.Log($"[ServerRecord] Connect using direct address {DirectAddress}:{DirectPort}");
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
                multiPass?.SetClientTransport<UnityTransport>();
                Debug.Log($"[ServerRecord] Connect using relay code {RelayJoinCode}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
    }
}
