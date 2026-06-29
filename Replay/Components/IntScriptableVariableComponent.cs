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
    public partial class IntScriptableVariableComponent : AFrameSequenceComponent<IntScriptableVariable, IntScriptableVariableComponent.ComponentData>
    {
        public override string ComponentLabel => "Int Scriptable Variable";
        protected override string TargetID => "IntVar";

        public IntScriptableVariableComponent() { }
        public IntScriptableVariableComponent(IntScriptableVariable target, ComponentData data) : base(target, data) { }

        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(8)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<IntScriptableVariable, FramePreset>
        {
            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames) : base(id, frames) { }

            protected override FramePreset Capture(IntScriptableVariable target, float time) => new(time, target.Value);
            protected override void Apply(IntScriptableVariable target, FramePreset beforeFrame, FramePreset afterFrame, float blend) => target.Value = beforeFrame.Value;
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
            private int _Value;

            [MemoryPackIgnore]
            public int Value => _Value;

            [MemoryPackConstructor]
            public FramePreset() { }
            public FramePreset(float time, int value) : base(time)
            {
                _Value = value;
            }

            public override AFramePreset GetCopy() => new FramePreset(Time, Value);
        }
    }
}
