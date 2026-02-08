// Author: František Holubec
// Created: 08.02.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using EDIVE.Core;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Audio.Replay
{
    public class ReplayTestScript : MonoBehaviour
    {
        [SerializeField]
        private int _Frequency;
            
        [SerializeField]
        private int _ChannelCount;
            
        [SerializeField]
        private List<VoiceChatReplayAgentComponent.SerializedAudioFrame> _AudioFrames = new();
        
        private CancellationTokenSource _cancellationTokenSource;
        private AudioManager _audioManager;
        private LocalAudioSource _audioOutput;
        
        private List<IAudioFilter> _encodeFilters;
        private List<IAudioFilter> _decodeFilters;
        
        private long _startTimestamp;
        private int _playbackIndex;
        
        private const long AUDIO_LOOKAHEAD_MS = 100; // Lookahead for buffering

        private void Awake()
        {
            _audioOutput = LocalAudioSource.New();
            _audioOutput.gameObject.name = "ReplayAudioOutput";
            _audioOutput.transform.SetParent(transform, false);
            _audioOutput.transform.localPosition = Vector3.zero;
            var audioSource = _audioOutput.UnityAudioSource;
            audioSource.spatialBlend = 0; 
            audioSource.maxDistance = 1000;
            
            _decodeFilters = new List<IAudioFilter>
            {
                new ConcentusDecodeFilter()
            };
            
            _encodeFilters = new List<IAudioFilter>
            {
                new RNNoiseFilter(),
                new SimpleVadFilter(new SimpleVad()),
                new ConcentusEncodeFilter()
            };
        }

        [Button]
        public void StartRecording()
        {
            if (!AppCore.Services.TryGet(out _audioManager))
                return;
            StopRecording();
            StopPlayback();
            
            var unityAudioOutput = _audioOutput.UnityAudioSource;
            unityAudioOutput.volume = 0f;
            _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _Frequency = 0;
            _ChannelCount = 0;
            _audioManager.LocalAudioFrameReady += WriteAudioFrame;
            _audioManager.LocalAudioFrameReady += WriteMetadata;
        }
        
        [Button]
        public void StopRecording()
        {
            _audioManager.LocalAudioFrameReady -= WriteAudioFrame;
            _audioManager.LocalAudioFrameReady -= WriteMetadata;
        }
        
        private void WriteMetadata(AudioFrame audioFrame)
        {
            _audioManager.LocalAudioFrameReady -= WriteMetadata;
            _Frequency = audioFrame.frequency;
            _ChannelCount = audioFrame.channelCount;
        }

        private void WriteAudioFrame(AudioFrame audioFrame)
        {
            if (TryProcessAudioFrame(ref audioFrame, _encodeFilters)) 
                _AudioFrames?.Add(new VoiceChatReplayAgentComponent.SerializedAudioFrame(audioFrame.timestamp - _startTimestamp, audioFrame.samples));
        }

        [Button]
        public void StartPlayback()
        {
            StopRecording();
            StopPlayback();
            _audioOutput.Clear();
            
            var frames = _AudioFrames;
            if (frames.Count == 0)
                return;

            const int timelineOffsetMs = 0;
            _playbackIndex = 0;
            _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var unityAudioOutput = _audioOutput.UnityAudioSource;
            unityAudioOutput.volume = 1f;
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var cancellationToken = _cancellationTokenSource.Token;
            cancellationToken.Register(() =>
            {
                //unityAudioOutput.volume = 0f;
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
                            frequency = _Frequency,
                            channelCount =_ChannelCount,
                            samples = sample._Samples,
                        };
                        
                        if (TryProcessAudioFrame(ref audioFrame, _decodeFilters)) 
                            _audioOutput.Feed(audioFrame.frequency, audioFrame.channelCount, audioFrame.samples);
                        _playbackIndex++;
                    }
                })
                .RegisterTo(cancellationToken);
        }

        private void StopPlayback()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        
        private static bool TryProcessAudioFrame(ref AudioFrame frame, List<IAudioFilter> filters)
        {
            if (filters != null) {
                foreach (var filter in filters) {
                    frame = filter.Run(frame);
                    if (frame.samples == null)
                        break;
                }
            }

            return frame.samples != null && frame.samples.Length > 0;
        }
    }
}
