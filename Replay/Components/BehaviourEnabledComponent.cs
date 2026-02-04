// Author: František Holubec
// Created: 04.07.2025

using System;
using System.Collections.Generic;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Components
{
    [Serializable]
    public partial class BehaviourEnabledComponent : AFrameSequenceComponent<Behaviour, BehaviourEnabledComponent.ComponentData>
    {
        public override string ComponentLabel => "Behaviour Enabled";
        protected override string TargetID => "BhEnabled";

        public BehaviourEnabledComponent() { }
        public BehaviourEnabledComponent(Behaviour target, ComponentData data) : base(target, data) { }

        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(1)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<Behaviour, FramePreset>
        {
            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames) : base(id, frames) { }

            protected override FramePreset Capture(Behaviour target, float time) => new(time, target.enabled);
            protected override void Apply(Behaviour target, FramePreset beforeFrame, FramePreset afterFrame, float blend) => target.enabled = beforeFrame.Enabled;
            protected override bool AreValuesEqual(FramePreset a, FramePreset b) => a.Enabled == b.Enabled;
            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, GetFramesCopy());
        }
    
        [Serializable]
        [MemoryPackable]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class FramePreset : AFramePreset
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Enabled")]
            protected bool _Enabled;

            [MemoryPackIgnore]
            public bool Enabled => _Enabled;

            [MemoryPackConstructor]
            public FramePreset() { }
            public FramePreset(float time, bool enabled) : base(time)
            {
                _Enabled = enabled;
            }
            
            public override AFramePreset GetCopy() => new FramePreset(Time, Enabled);
        }
    }
}
