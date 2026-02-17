// Author: František Holubec
// Created: 16.02.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Replay.Agents;
using EDIVE.Replay.Strategies;
using FishNet;
using FishNet.Object;
using UnityEngine;

namespace EDIVE.Replay.Network
{
    [Serializable]
    public class NetworkReplayAgentSpawnStrategy : AReplayAgentSpawnStrategy
    {
        [SerializeField]
        private NetworkObject _Prefab;
        
        public override bool IsValidFor(IPlaybackContext context)
        {
            return context is NetworkPlaybackContext netContext && netContext.NetworkManager.IsServerStarted;
        }
        
        public override UniTask<(bool, ReplayAgentHandler)> TrySpawnObjectAsync(IPlaybackContext context, CancellationToken cancellationToken = default)
        {
            if (context is not NetworkPlaybackContext netContext)
                return UniTask.FromResult((false, (ReplayAgentHandler) null));
                
            var networkManager = netContext.NetworkManager;
            var netObj = networkManager.GetPooledInstantiated(_Prefab, true);
            networkManager.ServerManager.Spawn(netObj, InstanceFinder.ClientManager.Connection);
            
            if (!netObj.TryGetComponent<ReplayAgentHandler>(out var handler))
                return UniTask.FromResult((false, (ReplayAgentHandler) null));
                
            return UniTask.FromResult((true, handler));
        }
    }
}
