// Author: František Holubec
// Created: 16.02.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Replay.Agents;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Replay.Strategies
{
    [Serializable]
    public class PrefabReplayAgentSpawnStrategy : AReplayAgentSpawnStrategy
    {
        [SerializeField]
        private ReplayAgentHandler _Prefab;

        public PrefabReplayAgentSpawnStrategy() { }
        public PrefabReplayAgentSpawnStrategy(ReplayAgentHandler prefab)
        {
            _Prefab = prefab;
        }

        public override UniTask<(bool, ReplayAgentHandler)> TrySpawnObjectAsync(CancellationToken cancellationToken = default)
        {
            var handler = Object.Instantiate(_Prefab);
            handler.SetDespawnDelegate(h => Object.Destroy(h.gameObject));
            return UniTask.FromResult((true, handler));
        }
    }
}
