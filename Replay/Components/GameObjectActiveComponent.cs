// Author: František Holubec
// Created: 04.07.2025

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Replay.Agents;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Components
{
    [Serializable]
    public partial class GameObjectActiveComponent : AFrameSequenceComponent<GameObject, GameObjectActiveComponent.ComponentData>
    {
        public override string ComponentLabel => "GameObject Active";
        protected override string TargetID => "GOActive";
        
        public GameObjectActiveComponent() { }
        public GameObjectActiveComponent(GameObject target, ComponentData data) : base(target, data) { }

        private bool _hasRestoreState;
        private bool _restoreActive;

        public override UniTask PreparePlayback(float startTime, CancellationToken cancellationToken = default)
        {
            if (_Target != null && !_hasRestoreState)
            {
                _restoreActive = _Target.activeSelf;
                _hasRestoreState = true;
            }
            return base.PreparePlayback(startTime, cancellationToken);
        }

        public override void OnPlaybackUnloaded()
        {
            if (_hasRestoreState && _Target != null && _Target.activeSelf != _restoreActive)
                _Target.SetActive(_restoreActive);
            _hasRestoreState = false;
        }

        public override void OnAgentTerminating() => _Data?.MarkTerminating();
        
        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(2)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AFrameSequenceComponentData<GameObject, FramePreset>
        {
            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<FramePreset> frames) : base(id, frames) { }

            private bool _terminating;

            public void MarkTerminating() => _terminating = true;

            public override void StartRecording(float startTime, GameObject target, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
            {
                _terminating = false;
                // Set inactive for previous frames if started during recording.
                if (startTime != 0)
                    _Frames.Add(new FramePreset(startTime, false));

                base.StartRecording(startTime, target, config, cancellationToken);
                // Destroyed object ends inactive, normal stop keeps real state
                cancellationToken.Register(() =>
                {
                    var active = !_terminating && target != null && target.activeSelf;
                    _Frames.Add(new FramePreset(UnityEngine.Time.time - _startTimestamp + startTime, active));
                });
            }

            protected override FramePreset Capture(GameObject target, float time) => new(time, target.activeSelf);

            protected override void Apply(GameObject target, FramePreset beforeFrame, FramePreset afterFrame, float blend)
            {
                if (target.activeSelf != beforeFrame.Active)
                    target.SetActive(beforeFrame.Active);
            }

            protected override bool AreValuesEqual(FramePreset a, FramePreset b) => a.Active == b.Active;
            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, GetFramesCopy());
        }
        
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
