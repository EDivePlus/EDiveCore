// Author: František Holubec
// Created: 24.02.2026

using System;
using EDIVE.Replay.Strategies;

namespace EDIVE.Replay
{
    [Serializable]
    public class SimpleReplayHandler : ReplayHandler<PrefabReplayAgentSpawnStrategy>
    {
        public SimpleReplayHandler() { }
        public SimpleReplayHandler(ReplayScope scope) : base(scope) { }
    }
}
