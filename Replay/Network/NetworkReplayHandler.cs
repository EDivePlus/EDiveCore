// Author: František Holubec
// Created: 24.02.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay.Network
{
    [Serializable]
    public class NetworkReplayHandler : IReplayHandler
    {
        [Required]
        [SerializeField]
        private NetworkReplayProxy _Proxy;
        
        public event Action TimeChanged;
        public event Action StateChanged;
        
        public void Initialize()
        {
            _Proxy.StateChanged += StateChanged;
            _Proxy.TimeChanged += TimeChanged;
        }

        public void Terminate()
        {
            _Proxy.StateChanged -= StateChanged;
            _Proxy.TimeChanged -= TimeChanged;
        }

        public float CurrentDuration => _Proxy.CurrentDuration;
        public bool HasAnyDuration => CurrentDuration > 0f;
        public float CurrentTime => _Proxy.CurrentTime;
        public bool IsRecording => _Proxy.IsRecording;
        
        public void StartRecording() => _Proxy.StartRecording();
        public void StopRecording() => _Proxy.StopRecording();
        public void ResetRecording() => _Proxy.ResetRecording();
        public void SetRecordingTime(float time, bool clearFollowingFrames = true) => _Proxy.SetRecordingTime(time, clearFollowingFrames);

        public PlaybackLoadState PlaybackLoadState => _Proxy.PlaybackLoadState;
        public bool IsPlaybackLoaded => PlaybackLoadState == PlaybackLoadState.Loaded;
        public bool IsPlaybackLoading => PlaybackLoadState == PlaybackLoadState.Loading;
        public bool IsPlaybackPlaying => _Proxy.IsPlaybackPlaying;
        
        public void StartPlayback() => _Proxy.StartPlayback();
        public void StopPlayback() => _Proxy.StopPlayback();
        public void SetPlaybackTime(float newTime) => _Proxy.SetPlaybackTime(newTime);
        public void UnloadPlayback() => _Proxy.UnloadPlayback();
        
        public bool IsLoadingRecord => _Proxy.IsLoadingRecord;
        
        public void SaveCurrentRecord() => _Proxy.SaveCurrentRecord();
        public void LoadRecord(ReplayRecordInfo info) => _Proxy.LoadRecord(info);
        public UniTask<IEnumerable<ReplayRecordInfo>> GetSavedRecords() => _Proxy.GetSavedRecords();
    }
}
