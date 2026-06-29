// Author: František Holubec
// Created: 29.06.2026

using System;
using System.Collections.Generic;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Components
{
    [Serializable]
    public partial class RigidbodyPositionRotationComponent : AFrameSequenceComponent<Rigidbody, RigidbodyPositionRotationComponent.ComponentData>
    {
        public override string ComponentLabel => "Rigidbody Position & Rotation";
        protected override string TargetID => "RbPosRot";

        public RigidbodyPositionRotationComponent() { }
        public RigidbodyPositionRotationComponent(Rigidbody target, ComponentData data) : base(target, data) { }

        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(5)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<Rigidbody, FramePreset>
        {
            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames) : base(id, frames) { }

            protected override FramePreset Capture(Rigidbody target, float time) => new(time, target.position, target.rotation);

            protected override void Apply(Rigidbody target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
            {
                target.position = Vector3.Lerp(beforeFrame.Position, afterFrame.Position, blend);
                target.rotation = Quaternion.Slerp(beforeFrame.Rotation, afterFrame.Rotation, blend);
            }

            protected override bool AreValuesEqual(FramePreset a, FramePreset b) => a.Position == b.Position && a.Rotation == b.Rotation;
            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, GetFramesCopy());
        }

        [Serializable]
        [MemoryPackable]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class FramePreset : AFramePreset
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Position")]
            private Vector3 _Position;

            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Rotation")]
            private Quaternion _Rotation;

            [MemoryPackIgnore]
            public Vector3 Position => _Position;
            [MemoryPackIgnore]
            public Quaternion Rotation => _Rotation;

            [MemoryPackConstructor]
            public FramePreset() { }
            public FramePreset(float time, Vector3 position, Quaternion rotation) : base(time)
            {
                _Position = position;
                _Rotation = rotation;
            }

            public override AFramePreset GetCopy() => new FramePreset(Time, Position, Rotation);
        }
    }
}
