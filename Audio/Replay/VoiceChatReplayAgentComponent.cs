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
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Audio.Replay
{
    [Serializable]
    public partial class VoiceChatReplayAgentComponent : AReplayAgentComponent<VoiceChatReplayProxy, VoiceChatReplayAgentComponent.ComponentData>
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
            _Data.SetMetadata(0, 0);
            _Target.AudioFrameReceived += WriteAudioFrame;
            _Target.AudioFrameReceived += WriteMetadata;
            cancellationToken.Register(() =>
            {
                _Target.AudioFrameReceived -= WriteAudioFrame;
                _Target.AudioFrameReceived -= WriteMetadata;
            });
        }
        
        private void WriteMetadata(AudioFrame audioFrame)
        {
            _Target.AudioFrameReceived -= WriteMetadata;
            _Data?.SetMetadata(audioFrame.frequency, audioFrame.channelCount);
        }

        private void WriteAudioFrame(AudioFrame audioFrame)
        {
            _Data?.AddFrame(new SerializedAudioFrame(audioFrame.timestamp - _startTimestamp, audioFrame.samples));
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
                    while (_playbackIndex < frames.Count && frames[_playbackIndex]._Timestamp <= targetTimeMs)
                    {
                        var sample = frames[_playbackIndex];
                        var audioFrame = new AudioFrame
                        {
                            timestamp = sample._Timestamp,
                            frequency = _Data.Frequency,
                            channelCount = _Data.ChannelCount,
                            samples = sample._Samples,
                           
                        };
                        _Target.FeedAudioFrame(audioFrame);
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
            _Data.ClearFrames(f => f._Timestamp >= startTime);
        }
        
        [Serializable]
        [MemoryPackable]
        [JsonObject(MemberSerialization.OptIn)]
        public partial struct SerializedAudioFrame 
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Timestamp")]
            public long _Timestamp;

            [HideInInspector]
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Samples")]
            public byte[] _Samples;

            public SerializedAudioFrame(long timestamp, byte[] samples)
            {
                _Timestamp = timestamp;
                _Samples = samples;
            }
            
            [ShowInInspector]
            [ReadOnly]
            private int SamplesLength => _Samples?.Length ?? 0;
        }
        
        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(21)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AReplayAgentComponentData
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Frequency")]
            protected int _Frequency;
            
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("ChannelCount")]
            protected int _ChannelCount;
            
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("AudioFrames")]
            protected List<SerializedAudioFrame> _AudioFrames = new();

            [MemoryPackIgnore]
            public int Frequency => _Frequency;

            [MemoryPackIgnore]
            public int ChannelCount => _ChannelCount;

            [MemoryPackIgnore]
            public List<SerializedAudioFrame> AudioFrames => _AudioFrames;

            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<SerializedAudioFrame> audioFrames) : base(id)
            {
                _AudioFrames = audioFrames;
            }

            public override float GetMinTime() => _AudioFrames != null && _AudioFrames.Any() ? _AudioFrames.First()._Timestamp : 0f;
            public override float GetMaxTime() => _AudioFrames != null && _AudioFrames.Any() ? _AudioFrames.Last()._Timestamp : 0f;

            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, _AudioFrames);
            
            public void ClearFrames(Predicate<SerializedAudioFrame> predicate)
            {
                _AudioFrames.RemoveAll(predicate);
            }
            
            public void AddFrame(SerializedAudioFrame frame)
            {
                _AudioFrames.Add(frame);
            }
            
            public void SetMetadata(int frequency, int channelCount)
            {
                _Frequency = frequency;
                _ChannelCount = channelCount;
            }
            
            public int BinarySearchFrame(long timestampMs)
            {
                var lo = 0;
                var hi = _AudioFrames.Count;
            
                while (lo < hi)
                {
                    var mid = lo + (hi - lo) / 2;
                    if (_AudioFrames[mid]._Timestamp < timestampMs)
                        lo = mid + 1;
                    else
                        hi = mid;
                }
                return lo;
            }
        }
    }
}
