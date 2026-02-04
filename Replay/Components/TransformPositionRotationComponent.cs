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
    public partial class TransformPositionRotationComponent : AFrameSequenceComponent<Transform, TransformPositionRotationComponent.ComponentData>
    {
        public override string ComponentLabel => "Transform Position & Rotation";
        protected override string TargetID => "TrPosRot";

        public TransformPositionRotationComponent() { }
        public TransformPositionRotationComponent(Transform target, ComponentData data) : base(target, data) { }
        
        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(3)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<Transform, FramePreset>
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("UseGlobal")]
            private bool _UseGlobal;

            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames, bool useGlobal) : base(id, frames)
            {
                _UseGlobal = useGlobal;
            }

            protected override FramePreset Capture(Transform target, float time)
            {
                return _UseGlobal ? new FramePreset(time, target.position, target.rotation) : new FramePreset(time, target.localPosition, target.localRotation);
            }

            protected override void Apply(Transform target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
            {
                var position = Vector3.Lerp(beforeFrame.Position, afterFrame.Position, blend);
                var rotation = Quaternion.Slerp(beforeFrame.Rotation, afterFrame.Rotation, blend);

                if (_UseGlobal)
                {
                    target.position = position;
                    target.rotation = rotation;
                }
                else
                {
                    target.localPosition = position;
                    target.localRotation = rotation;
                }
            }

            protected override bool AreValuesEqual(FramePreset a, FramePreset b) => a.Position == b.Position && a.Rotation == b.Rotation;
            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, GetFramesCopy(), _UseGlobal);
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
