// Author: František Holubec
// Created: 04.07.2025

using System;
using System.Collections.Generic;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Frames
{
    [Serializable]
    [MemoryPackable, MemoryPackUnionTag(3)]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class TransformPositionRotationFrameSequence : AFrameSequence<Transform, TransformPositionRotationFrameSequence.FramePreset>
    {
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("UseGlobal")]
        private bool _UseGlobal;

        public override string Title => "Position & Rotation";

        [MemoryPackConstructor]
        public TransformPositionRotationFrameSequence() { }
        public TransformPositionRotationFrameSequence(List<FramePreset> frames, bool useGlobal) : base(frames)
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
        public override AFrameSequence GetCopy() => new TransformPositionRotationFrameSequence(GetFramesCopy(), _UseGlobal);

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

    [Serializable]
    [MemoryPackable, MemoryPackUnionTag(4)]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class TransformScaleFrameSequence : AFrameSequence<Transform, TransformScaleFrameSequence.FramePreset>
    {
        public override string Title => "Scale";

        [MemoryPackConstructor]
        public TransformScaleFrameSequence() { }
        public TransformScaleFrameSequence(List<FramePreset> frames) : base(frames) { }

        protected override FramePreset Capture(Transform target, float time) => new(time, target.localScale);

        protected override void Apply(Transform target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
        {
            target.localScale = Vector3.Lerp(beforeFrame.Scale, afterFrame.Scale, blend);
        }

        protected override bool AreValuesEqual(FramePreset a, FramePreset b) => a.Scale == b.Scale;
        public override AFrameSequence GetCopy() => new TransformScaleFrameSequence(GetFramesCopy());

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
