// Author: František Holubec
// Created: 16.02.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Replay.Agents;

namespace EDIVE.Replay.Strategies
{
    [Serializable]
    public abstract class AReplayAgentSpawnStrategy
    {
        public abstract UniTask<(bool, ReplayAgentHandler)> TrySpawnObjectAsync(CancellationToken cancellationToken = default);
    }
}
