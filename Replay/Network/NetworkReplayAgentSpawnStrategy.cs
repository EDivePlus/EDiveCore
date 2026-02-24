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

        public NetworkReplayAgentSpawnStrategy() { }
        public NetworkReplayAgentSpawnStrategy(NetworkObject prefab)
        {
            _Prefab = prefab;
        }

        public override UniTask<(bool, ReplayAgentHandler)> TrySpawnObjectAsync(CancellationToken cancellationToken = default)
        {
            var networkManager = InstanceFinder.NetworkManager;
            var netObj = networkManager.GetPooledInstantiated(_Prefab, true);
            networkManager.ServerManager.Spawn(netObj, InstanceFinder.ClientManager.Connection);
            
            if (!netObj.TryGetComponent<ReplayAgentHandler>(out var handler))
                return UniTask.FromResult((false, (ReplayAgentHandler) null));
                
            handler.SetDespawnDelegate(h => networkManager.ServerManager.Despawn(h.gameObject));
            return UniTask.FromResult((true, handler));
        }
    }
}
