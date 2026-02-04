// Author: František Holubec
// Created: 04.02.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Adrenak.UniVoice;
using EDIVE.Replay;
using EDIVE.Replay.Components;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using R3;
using UnityEngine;

namespace EDIVE.Audio.Replay
{
    [Serializable]
    public partial class VoiceChatReplayAgentComponent : AReplayAgentComponent<ReplayAudioProxy, VoiceChatReplayAgentComponent.ComponentData>
    {
        public override string ComponentLabel => "Voice Chat Audio";
        protected override string TargetID => "VCAudio";
        
        protected long _startTimestamp;
        private int _playbackIndex;
        
        private const long AUDIO_LOOKAHEAD_MS = 100; // Lookahead for buffering
        
        public override void StartRecording(float startTime, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
        {
            if (_Data == null || _Target == null)
                return;
            
            _Target.UnityAudioSource.volume = 0f;
            _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _Target.AudioFrameReceived += OnAudioFrameReceived;
            cancellationToken.Register(() =>
            {
                _Target.AudioFrameReceived -= OnAudioFrameReceived;
            });
        }

        private void OnAudioFrameReceived(AudioFrame audioFrame)
        {
            if (_Data == null || _Target == null)
                return;
            
            audioFrame.timestamp -= _startTimestamp;
            _Data.AddFrame(audioFrame);
        }
        
        public override void StartPlayback(float startTime, CancellationToken cancellationToken = default)
        {
            if (_Data == null || _Target == null)
                return;

            var frames = _Data.AudioFrames;
            if (frames.Count == 0)
                return;
            
            var timelineOffsetMs = (long) startTime;
            _playbackIndex = _Data.BinarySearchFrame(timelineOffsetMs);
            _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _Target.UnityAudioSource.volume = 1f;
            cancellationToken.Register(() =>
            {
                _Target.UnityAudioSource.volume = 0f;
            });
            
            Observable
                .EveryUpdate(cancellationToken)
                .Subscribe(_ =>
                {
                    var elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTimestamp + timelineOffsetMs;
                    
                    // Offset for buffering
                    var targetTimeMs = elapsedMs + AUDIO_LOOKAHEAD_MS;
                    
                    // Feed all frames up to the lookahead time
                    while (_playbackIndex < frames.Count && frames[_playbackIndex].timestamp <= targetTimeMs)
                    {
                        _Target.FeedAudioFrame(frames[_playbackIndex]);
                        _playbackIndex++;
                    }
                })
                .RegisterTo(cancellationToken);
        }
        
        public override void ApplyTime(float time)
        {
            // Nothing to apply, audio frames are fed in real-time during playback.
        }

        public override void ClearData(float startTime = 0)
        {
            _Data.ClearFrames(f => f.timestamp >= startTime);
        }
        
        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(21)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AReplayAgentComponentData
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("AudioFrames")]
            protected List<AudioFrame> _AudioFrames = new();

            public IReadOnlyList<AudioFrame> AudioFrames => _AudioFrames;
            
            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<AudioFrame> audioFrames) : base(id)
            {
                _AudioFrames = audioFrames;
            }

            public override float GetMinTime() => _AudioFrames != null && _AudioFrames.Any() ? _AudioFrames.First().timestamp : 0f;
            public override float GetMaxTime() => _AudioFrames != null && _AudioFrames.Any() ? _AudioFrames.Last().timestamp : 0f;

            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, _AudioFrames);
            
            public void ClearFrames(Predicate<AudioFrame> predicate)
            {
                _AudioFrames.RemoveAll(predicate);
            }
            
            public void AddFrame(AudioFrame frame)
            {
                _AudioFrames.Add(frame);
            }
            
            public int BinarySearchFrame(long timestampMs)
            {
                var lo = 0;
                var hi = _AudioFrames.Count;
            
                while (lo < hi)
                {
                    var mid = lo + (hi - lo) / 2;
                    if (_AudioFrames[mid].timestamp < timestampMs)
                        lo = mid + 1;
                    else
                        hi = mid;
                }
                return lo;
            }
        }
    }
}
