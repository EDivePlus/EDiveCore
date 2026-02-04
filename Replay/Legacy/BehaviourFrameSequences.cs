// Author: František Holubec
// Created: 04.07.2025

using System;
using EDIVE.Replay.Components;
using UnityEngine;

namespace EDIVE.Replay.Frames
{
    [Serializable]
    public class BehaviourEnabledFrameSequence : AFrameSequence<Behaviour, BehaviourEnabledComponent.FramePreset >
    {
        public override AReplayAgentComponent Migrate(ReplayAgentComponent component)
        {
            return new BehaviourEnabledComponent(component.Target as Behaviour, new BehaviourEnabledComponent.ComponentData(component.ID, _Frames));
        }
    }
}
