// Author: František Holubec
// Created: 04.07.2025

using System;
using EDIVE.Replay.Components;
using UnityEngine;

namespace EDIVE.Replay.Frames
{
    [Serializable]
    public class TransformPositionRotationFrameSequence : AFrameSequence<Transform, TransformPositionRotationComponent.FramePreset>
    {
        [SerializeField]
        private bool _UseGlobal;
        
        public override AReplayAgentComponent Migrate(ReplayAgentComponent component)
        {
            return new TransformPositionRotationComponent(component.Target as Transform, new TransformPositionRotationComponent.ComponentData(component.ID, _Frames, _UseGlobal));
        }
    }

    [Serializable]
    public class TransformScaleFrameSequence : AFrameSequence<Transform, TransformScaleComponent.FramePreset>
    {
        public override AReplayAgentComponent Migrate(ReplayAgentComponent component)
        {
            return new TransformScaleComponent(component.Target as Transform, new TransformScaleComponent.ComponentData(component.ID, _Frames));
        }
    }
}
