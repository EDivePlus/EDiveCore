// Author: František Holubec
// Created: 21.07.2025

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AssetTranslation;
using EDIVE.Replay.Strategies;
using UnityEngine;

namespace EDIVE.Replay.Agents
{
    public class ReplayAgentDefinition : AUniqueDefinition
    {
        [SerializeReference]
        private List<AReplayAgentSpawnStrategy> _SpawnStrategies;
        
        [Obsolete("Use spawn strategies instead")]
        [SerializeField]
        private ReplayAgentHandler _Prefab;
        
        public async UniTask<(bool, ReplayAgentHandler)> TrySpawnObjectAsync(IPlaybackContext context, CancellationToken cancellationToken = default)
        {
            foreach (var strategy in _SpawnStrategies)
            {
                if (strategy.IsValidFor(context))
                    return await strategy.TrySpawnObjectAsync(context, cancellationToken);
            }

            return (false, null);
        }
    }
}
