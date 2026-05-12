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
        // TODO PurrNet migration: the inspector reference will lose its prefab when changing
        // the field type from FishNet's NetworkObject to PurrNet's NetworkIdentity. Re-assign
        // the prefab in the inspector and make sure it has a NetworkIdentity component.
        [SerializeField]
        private NetworkIdentity _Prefab;

        public NetworkReplayAgentSpawnStrategy() { }
        public NetworkReplayAgentSpawnStrategy(NetworkIdentity prefab)
        {
            _Prefab = prefab;
        }

        public override UniTask<(bool, ReplayAgentHandler)> TrySpawnObjectAsync(CancellationToken cancellationToken = default)
        {
            // PurrNet auto-spawns identities on Instantiate. Pooling support exists via
            // NetworkIdentity.shouldBePooled but is not wired up here (FishNet's GetPooledInstantiated
            // had no 1:1 equivalent we relied on); revisit if pooling matters for performance.
            var netObj = UnityEngine.Object.Instantiate(_Prefab);

            // Match FishNet semantics: give ownership to the local player (works on host;
            // on a dedicated server localPlayer is default, so no owner is assigned).
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
