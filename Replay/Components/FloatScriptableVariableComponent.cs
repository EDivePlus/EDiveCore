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
    public partial class FloatScriptableVariableComponent : AFrameSequenceComponent<FloatScriptableVariable, FloatScriptableVariableComponent.ComponentData>
    {
        public override string ComponentLabel => "Float Scriptable Variable";
        protected override string TargetID => "FloatVar";

        public FloatScriptableVariableComponent() { }
        public FloatScriptableVariableComponent(FloatScriptableVariable target, ComponentData data) : base(target, data) { }

        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(9)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<FloatScriptableVariable, FramePreset>
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Interpolate")]
            private bool _Interpolate = true;

            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames) : base(id, frames) { }

            protected override FramePreset Capture(FloatScriptableVariable target, float time) => new(time, target.Value);

            protected override void Apply(FloatScriptableVariable target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
            {
                target.Value = _Interpolate ? Mathf.Lerp(beforeFrame.Value, afterFrame.Value, blend) : beforeFrame.Value;
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
            private float _Value;

            [MemoryPackIgnore]
            public float Value => _Value;

            [MemoryPackConstructor]
            public FramePreset() { }
            public FramePreset(float time, float value) : base(time)
            {
                _Value = value;
            }

            public override AFramePreset GetCopy() => new FramePreset(Time, Value);
        }
    }
}
