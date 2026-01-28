// Author: František Holubec
// Created: 04.07.2025

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Frames
{
    [Serializable]
    [MemoryPackable, MemoryPackUnionTag(2)]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class GameObjectActiveFrameSequence : AFrameSequence<GameObject, GameObjectActiveFrameSequence.FramePreset>
    {
        public override string Title => "Active";

        [MemoryPackConstructor]
        public GameObjectActiveFrameSequence() { }
        public GameObjectActiveFrameSequence(List<FramePreset> frames) : base(frames) { }

        private ReplayAgent _currentAgent;

        protected override void StartCapture(float startTime, GameObject target, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
        {
            // Set inactive for previous frames if started during recording.
            if (startTime != 0) 
                _Frames.Add(new FramePreset(startTime, false));
            
            base.StartCapture(startTime, target, config, cancellationToken);
            cancellationToken.Register(() =>
            {
                _Frames.Add(new FramePreset(UnityEngine.Time.time - _startCaptureTime + startTime, false));
            });
        }

        protected override FramePreset Capture(GameObject target, float time)
        {
            return new FramePreset(time, target.activeSelf);
        }

        protected override void Apply(GameObject target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
        {
            if (target.activeSelf != beforeFrame.Active)
                target.SetActive(beforeFrame.Active);
        }

        protected override bool AreValuesEqual(FramePreset a, FramePreset b) => a.Active == b.Active;
        public override AFrameSequence GetCopy() => new GameObjectActiveFrameSequence(GetFramesCopy());

        [Serializable]
        [MemoryPackable]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class FramePreset : AFramePreset
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Active")]
            protected bool _Active;

            [MemoryPackIgnore]
            public bool Active => _Active;

            [MemoryPackConstructor]
            private FramePreset() { }
            public FramePreset(float time, bool active) : base(time)
            {
                _Active = active;
            }
            
            public override AFramePreset GetCopy() => new FramePreset(Time, Active);
        }
    }
}
