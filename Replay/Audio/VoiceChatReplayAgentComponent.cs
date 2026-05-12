// Author: František Holubec
// Created: 04.02.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Adrenak.UniVoice;
using Cysharp.Threading.Tasks;
using EDIVE.Audio;
using EDIVE.Core;
using EDIVE.NativeUtils;
using EDIVE.Replay.Components;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using PurrNet;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EDIVE.Replay.Audio
{
    [Serializable]
    [MovedFrom(true, "EDIVE.Audio.Replay", "EDIVE.Audio.Replay", "VoiceChatReplayAgentComponent")]
    public partial class VoiceChatReplayAgentComponent : AReplayAgentComponent<VoiceChatReplayAgentComponent.ComponentData>
    {
        [SerializeField]
        private ReplayAudioOutput _AudioOutput;

        public override string ComponentLabel => "Voice Chat Audio";
        protected override string TargetID => "VCAudio";

        protected override GameObject TargetGameObject => _AudioOutput.TryGetGameObject(out var go) ? go : null;
        public override Type EditorTargetType => typeof(AudioSource);

        protected long _startTimestamp;
        private int _playbackIndex;
        private PlayerID _ownerUserID;
        
        public override void StartRecording(float startTime, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
        {
            if (_Data == null || !AppCore.Services.TryGet<AudioManager>(out var audioManager))
                return;

            if (!TargetGameObject.TryGetComponent<NetworkBehaviour>(out var networkBehaviour))
            {
                Debug.LogError("VoiceChatReplayAgentComponent requires a NetworkBehaviour attached to the target GameObject!");
                return;
            }
            
            _ownerUserID = networkBehaviour.owner!.Value;
            _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            audioManager.UserAudioFrameReady += WriteAudioFrame;
            cancellationToken.Register(() =>
            {
                if (AppCore.Services.TryGet(out audioManager))
                    audioManager.UserAudioFrameReady -= WriteAudioFrame;
            });
        }

        private void WriteAudioFrame(PlayerID clientID, AudioFrame audioFrame)
        {
            if (_ownerUserID != clientID)
                return;

            audioFrame.timestamp -= _startTimestamp;
            _Data?.AddFrame(audioFrame);
        }

        public override UniTask PreparePlayback(float startTime, CancellationToken cancellationToken = default)
        {
            if (_Data == null || _AudioOutput == null)
                return UniTask.CompletedTask;

            _AudioOutput.SetPlaybackEnabled(false);
            var timelineOffsetMs = (long)(startTime * 1000f);
            _playbackIndex = -1;
            if (!_Data.BinarySearchFrameIndex(timelineOffsetMs, out _playbackIndex))
                return UniTask.CompletedTask;
            
            FeedBufferedData(timelineOffsetMs + _AudioOutput.InitialBufferSize);
            return UniTask.CompletedTask;
        }

        public override void StartPlayback(float startTime, CancellationToken cancellationToken = default)
        {
            if (_playbackIndex < 0)
                return;
            
            var timelineOffsetMs = (long)(startTime * 1000f);
            _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            cancellationToken.Register(() =>
            {
                _AudioOutput.SetPlaybackEnabled(false);
            });
            
            _AudioOutput.SetPlaybackEnabled(true);
            Observable
                .EveryUpdate(cancellationToken)
                .Subscribe(_ =>
                {
                    var elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTimestamp + timelineOffsetMs + _AudioOutput.InitialBufferSize;
                    FeedBufferedData(elapsedMs);
                })
                .RegisterTo(cancellationToken);
        }
        
        private void FeedBufferedData(float targetMs)
        {
            while (_Data.TryGetFrame(_playbackIndex, out var audioFrame) && audioFrame.timestamp <= targetMs)
            {
                _AudioOutput.Feed(audioFrame);
                _playbackIndex++;
            }
        }
        
        public override void ApplyTime(float time)
        {
            // Nothing to apply, audio frames are fed in real-time during playback.
        }

        public override void ClearData(float startTime = 0)
        {
            _Data?.ClearFrames((long)(startTime * 1000f));
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
        [MemoryPackable]
        [JsonObject(MemberSerialization.OptIn)]
        public partial struct SerializedAudioConfig
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Timestamp")]
            public int _FrameIndex;

            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Frequency")]
            public int _Frequency;
            
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("ChannelCount")]
            public int _ChannelCount;

            public SerializedAudioConfig(int frameIndex, int frequency, int channelCount)
            {
                _FrameIndex = frameIndex;
                _Frequency = frequency;
                _ChannelCount = channelCount;
            }

            public bool Equals(SerializedAudioConfig other)
            {
                return _Frequency == other._Frequency && _ChannelCount == other._ChannelCount;
            }

            public override bool Equals(object obj)
            {
                return obj is SerializedAudioConfig other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_Frequency, _ChannelCount);
            }
        }

        [Serializable]
        [MemoryPackable, MemoryPackUnionTag(21)]
        [JsonObject(MemberSerialization.OptIn)]
        public partial class ComponentData : AReplayAgentComponentData
        {
            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("AudioFrames")]
            private List<SerializedAudioFrame> _AudioFrames = new();

            [SerializeField]
            [MemoryPackInclude]
            [JsonProperty("Configs")]
            private List<SerializedAudioConfig> _Configs = new();

            [MemoryPackConstructor]
            public ComponentData() { }
            public ComponentData(string id, List<SerializedAudioFrame> audioFrames, List<SerializedAudioConfig> configs) : base(id)
            {
                _AudioFrames = audioFrames;
                _Configs = configs;
            }
            
            public override float GetMinTime() => _AudioFrames != null && _AudioFrames.Any() ? _AudioFrames.First()._Timestamp : 0f;
            public override float GetMaxTime() => _AudioFrames != null && _AudioFrames.Any() ? _AudioFrames.Last()._Timestamp : 0f;

            public override AReplayAgentComponentData GetCopy() => new ComponentData(ID, _AudioFrames.ToList(), _Configs.ToList());
            
            public void ClearFrames(long timestampMs)
            {
                if (BinarySearchFrameIndex(timestampMs, out var frameIndex))
                {
                    if (frameIndex < _AudioFrames.Count)
                    {
                        _AudioFrames.RemoveRange(frameIndex, _AudioFrames.Count - frameIndex);
                        _Configs.RemoveAll(c => c._FrameIndex >= frameIndex);
                    }
                }
            }
            
            public bool TryGetFrame(int frameIndex, out AudioFrame frame)
            {
                frame = default;
                if (_AudioFrames == null || frameIndex < 0 || frameIndex >= _AudioFrames.Count)
                    return false;
                
                var sample = _AudioFrames[frameIndex];
                var config = GetConfigAtFrame(frameIndex);
                        
                frame = new AudioFrame
                {
                    timestamp = sample._Timestamp,
                    frequency = config._Frequency,
                    channelCount = config._ChannelCount,
                    samples = sample._Samples,
                };
                return true;
            }
            
            public void AddFrame(AudioFrame frame)
            {
                var newConfig = new SerializedAudioConfig(_AudioFrames.Count, frame.frequency, frame.channelCount);
                if (_Configs.Count == 0 || !_Configs[^1].Equals(newConfig)) 
                    _Configs.Add(newConfig);
                
                _AudioFrames.Add(new SerializedAudioFrame(frame.timestamp, frame.samples));
            }

            private SerializedAudioConfig GetConfigAtFrame(int frameIndex)
            {
                return _Configs.TryGetLast(c => c._FrameIndex <= frameIndex, out var config) ? config : new SerializedAudioConfig(0, 48000, 1);
            }
            
            public bool BinarySearchFrameIndex(long timestampMs, out int index)
            {
                index = 0;
                if (_AudioFrames == null || _AudioFrames.Count == 0)
                    return false;
                
                if (timestampMs <= _AudioFrames[0]._Timestamp)
                    return true;
                
                if (timestampMs > _AudioFrames[^1]._Timestamp)
                    return false;

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

                index = hi;
                return true;
            }
        }
    }
}
