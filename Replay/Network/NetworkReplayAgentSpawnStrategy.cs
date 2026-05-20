// Author: František Holubec
// Created: 16.02.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Replay.Agents;
using EDIVE.Replay.Strategies;
using PurrNet;
using UnityEngine;

namespace EDIVE.Replay.Network
{
    [Serializable]
    public class NetworkReplayAgentSpawnStrategy : AReplayAgentSpawnStrategy
    {
        [SerializeField]
        private NetworkIdentity _Prefab;

        public NetworkReplayAgentSpawnStrategy() { }
        public NetworkReplayAgentSpawnStrategy(NetworkIdentity prefab)
        {
            _Prefab = prefab;
        }

        public override UniTask<(bool, ReplayAgentHandler)> TrySpawnObjectAsync(CancellationToken cancellationToken = default)
        {
            var netObj = UnityEngine.Object.Instantiate(_Prefab);
            
            var networkManager = NetworkManager.main;
            if (networkManager != null && networkManager.isLocalPlayerReady)
                netObj.GiveOwnership(networkManager.localPlayer);

            if (!netObj.TryGetComponent<ReplayAgentHandler>(out var handler))
                return UniTask.FromResult((false, (ReplayAgentHandler) null));

            handler.SetDespawnDelegate(h => UnityEngine.Object.Destroy(h.gameObject));
            return UniTask.FromResult((true, handler));
        }
    }
}
