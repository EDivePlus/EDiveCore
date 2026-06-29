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
    public partial class Vector3ScriptableVariableComponent : AFrameSequenceComponent<Vector3ScriptableVariable, Vector3ScriptableVariableComponent.ComponentData>
    {
        public override string ComponentLabel => "Vector3 Scriptable Variable";
        protected override string TargetID => "V3Var";

        public Vector3ScriptableVariableComponent() { }
        public Vector3ScriptableVariableComponent(Vector3ScriptableVariable target, ComponentData data) : base(target, data) { }

        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(6)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<Vector3ScriptableVariable, FramePreset>
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Interpolate")]
            private bool _Interpolate = true;

            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames) : base(id, frames) { }

            protected override FramePreset Capture(Vector3ScriptableVariable target, float time) => new(time, target.Value);

            protected override void Apply(Vector3ScriptableVariable target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
            {
                target.Value = _Interpolate ? Vector3.Lerp(beforeFrame.Value, afterFrame.Value, blend) : beforeFrame.Value;
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
            private Vector3 _Value;

            [MemoryPackIgnore]
            public Vector3 Value => _Value;

            [MemoryPackConstructor]
            public FramePreset() { }
            public FramePreset(float time, Vector3 value) : base(time)
            {
                _Value = value;
            }

            public override AFramePreset GetCopy() => new FramePreset(Time, Value);
        }
    }
}
