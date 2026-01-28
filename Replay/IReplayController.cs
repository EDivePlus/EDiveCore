// Author: František Holubec
// Created: 24.07.2025

using EDIVE.External.Signals;

namespace EDIVE.Replay
{
    public interface IReplayController
    {
        Signal TimeChanged { get; }
        Signal StateChanged { get; } 
        
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
        void SaveCurrentRecording();
    }
}
