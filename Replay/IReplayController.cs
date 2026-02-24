// Author: František Holubec
// Created: 24.07.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace EDIVE.Replay
{
    public interface IReplayHandler
    {
        event Action TimeChanged;
        event Action StateChanged;

        void Initialize();
        void Terminate();
        
        float CurrentDuration { get; }
        bool HasAnyDuration { get; }
        float CurrentTime { get; }
        bool IsRecording { get; }
        
        void StartRecording();
        void StopRecording();
        void ResetRecording();
        void SetRecordingTime(float time, bool clearFollowingFrames = true);

        PlaybackLoadState PlaybackLoadState { get; }
        bool IsPlaybackLoaded { get; }
        bool IsPlaybackLoading { get; }
        bool IsPlaybackPlaying { get; }

        void StartPlayback();
        void StopPlayback();
        void SetPlaybackTime(float newTime);
        void UnloadPlayback();

        bool IsLoadingRecord { get; }
        void SaveCurrentRecord();
        void LoadRecord(ReplayRecordInfo info);
        UniTask<IEnumerable<ReplayRecordInfo>> GetSavedRecords();
    }
}
