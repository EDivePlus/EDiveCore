// Author: František Holubec
// Created: 04.07.2025

using System;
using System.Collections.Generic;
using EDIVE.Replay.Components;
using UnityEngine;

namespace EDIVE.Replay.Frames
{
    [Serializable]
    public abstract partial class AFrameSequence
    {
        public abstract AReplayAgentComponent Migrate(ReplayAgentComponent component);
    }

    [Serializable]
    public abstract partial class AFrameSequence<TTarget, TPreset> : AFrameSequence where TPreset : AFramePreset
    {
        [SerializeField]
        protected List<TPreset> _Frames = new();
    }
}
