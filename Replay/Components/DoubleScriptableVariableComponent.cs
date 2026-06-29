// Author: František Holubec
// Created: 29.06.2026

using System;
using System.Collections.Generic;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Components
{
    [Serializable]
    public partial class DoubleScriptableVariableComponent : AFrameSequenceComponent<DoubleScriptableVariable, DoubleScriptableVariableComponent.ComponentData>
    {
        public override string ComponentLabel => "Double Scriptable Variable";
        protected override string TargetID => "DblVar";

        public DoubleScriptableVariableComponent() { }
        public DoubleScriptableVariableComponent(DoubleScriptableVariable target, ComponentData data) : base(target, data) { }

        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(7)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<DoubleScriptableVariable, FramePreset>
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Interpolate")]
            private bool _Interpolate = true;

            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames) : base(id, frames) { }

            protected override FramePreset Capture(DoubleScriptableVariable target, float time) => new(time, target.Value);

            protected override void Apply(DoubleScriptableVariable target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
            {
                target.Value = _Interpolate ? beforeFrame.Value + (afterFrame.Value - beforeFrame.Value) * blend : beforeFrame.Value;
            }

            protected override bool AreValuesEqual(FramePreset a, FramePreset b) => a.Value == b.Value;
            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, GetFramesCopy());
        }

        [Serializable]
        [MemoryPackable]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class FramePreset : AFramePreset
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Value")]
            private double _Value;

            [MemoryPackIgnore]
            public double Value => _Value;

            [MemoryPackConstructor]
            public FramePreset() { }
            public FramePreset(float time, double value) : base(time)
            {
                _Value = value;
            }

            public override AFramePreset GetCopy() => new FramePreset(Time, Value);
        }
    }
}
