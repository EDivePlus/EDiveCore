// Author: František Holubec
// Created: 04.02.2026

using System;
using System.Collections.Generic;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Components
{
    [Serializable]
    public partial class TransformScaleComponent : AFrameSequenceComponent<Transform, TransformScaleComponent.ComponentData>
    {
        public override string ComponentLabel => "Transform Scale";
        protected override string TargetID => "TrScale";
        
        public TransformScaleComponent() { }
        public TransformScaleComponent(Transform target, ComponentData data) : base(target, data) { }
        
        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(4)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<Transform, FramePreset>
        {
            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames) : base(id, frames) { }

            protected override FramePreset Capture(Transform target, float time) => new(time, target.localScale);

            protected override void Apply(Transform target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
            {
                target.localScale = Vector3.Lerp(beforeFrame.Scale, afterFrame.Scale, blend);
            }

            protected override bool AreValuesEqual(FramePreset a, FramePreset b) => a.Scale == b.Scale;
            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, GetFramesCopy());
        }
        
        [Serializable]
        [MemoryPackable]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class FramePreset : AFramePreset
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Scale")]
            private Vector3 _Scale;

            [MemoryPackIgnore]
            public Vector3 Scale => _Scale;

            [MemoryPackConstructor]
            public FramePreset() { }
            public FramePreset(float time, Vector3 scale) : base(time)
            {
                _Scale = scale;
            }
            
            public override AFramePreset GetCopy() => new FramePreset(Time, Scale);
        }
    }
}
