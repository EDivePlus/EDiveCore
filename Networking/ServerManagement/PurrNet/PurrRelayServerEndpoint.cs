// Author: František Holubec
// Created: 14.05.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Networking.Utils;
using PurrNet.Transports;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.PurrNet
{
    public class PurrRelayServerEndpoint : AServerEndpoint
    {
        public string RoomName;
        public override string EndpointText => RoomName;
        
        public override async UniTask<bool> PrepareForConnect()
        {
            await UniTask.CompletedTask;
            
            if (string.IsNullOrEmpty(RoomName))
                return false;
            
            if (!AppCore.Services.TryGet<TransportController>(out var transportController))
                return false;
            
            if (!NetworkUtils.TrySetClientTransport<PurrTransport>(out var purrTransport))
                return false;
            
            try
            {
                purrTransport.roomName = RoomName;

                Debug.Log($"[ServerEndpoint] Connect using PurrNet relay '{RoomName}'");
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
