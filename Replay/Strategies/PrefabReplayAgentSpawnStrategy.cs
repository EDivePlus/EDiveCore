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
        
        public override bool IsValidFor(IPlaybackContext context)
        {
            return context is PlaybackContext;
        }
        
        public override UniTask<(bool, ReplayAgentHandler)> TrySpawnObjectAsync(IPlaybackContext context, CancellationToken cancellationToken = default)
        {
            return UniTask.FromResult((true, Object.Instantiate(_Prefab)));
        }
    }
}
