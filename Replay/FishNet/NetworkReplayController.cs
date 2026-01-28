// Author: František Holubec
// Created: 19.08.2025

#if FISHNET
using EDIVE.External.Signals;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay
{
    public class NetworkReplayController : NetworkBehaviour, IReplayController
    {
    #region Shared
        [Required]
        [SerializeField]
        private ReplayController _ServerReplayController;
        
        public float CurrentDuration => Mathf.Max(CurrentTime, _currentDuration.Value);
        public float CurrentTime => _currentTime.Value;
        public bool HasAnyDuration => _currentDuration.Value > 0f;
        
        private readonly SyncVar<float> _currentDuration = new();
        private readonly SyncVar<float> _currentTime = new();
        
        public Signal TimeChanged { get; } = new();
        public Signal StateChanged { get; } = new();
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (_ServerReplayController == null)
            {
                Debug.LogError("ServerReplayController is not assigned on NetworkReplayController.");
                return;
            }
            
            _ServerReplayController.StateChanged.AddListener(OnServerStateChanged);
            _ServerReplayController.TimeChanged.AddListener(OnServerTimeChanged);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (_ServerReplayController == null)
                return;
            
            _ServerReplayController.StateChanged.RemoveListener(OnServerStateChanged);
            _ServerReplayController.TimeChanged.RemoveListener(OnServerTimeChanged);
        }
        
        [ObserversRpc(RunLocally = true)]
        private void OnServerStateChanged()
        {
            _isRecording.Value = _ServerReplayController.IsRecording;
            _playbackLoadState.Value = _ServerReplayController.PlaybackLoadState;
            _playbackPlaying.Value = _ServerReplayController.IsPlaybackPlaying;
            
            _currentDuration.Value = _ServerReplayController.CurrentDuration;
            _currentTime.Value = _ServerReplayController.CurrentTime;
            
            StateChanged.Dispatch();
        }

        [ObserversRpc(RunLocally = true)]
        private void OnServerTimeChanged()
        {
            _currentDuration.Value = _ServerReplayController.CurrentDuration;
            _currentTime.Value = _ServerReplayController.CurrentTime;
            
            TimeChanged.Dispatch();
        }
    #endregion
        
    #region Recording
        public bool IsRecording => _isRecording.Value;
        private readonly SyncVar<bool> _isRecording = new();

        [ServerRpc]
        public void StartRecording() => 
            _ServerReplayController.StartRecording();

        [ServerRpc]
        public void StopRecording() => 
            _ServerReplayController.StartRecording();

        [ServerRpc]
        public void ResetRecording() => 
            _ServerReplayController.ResetRecording();

        [ServerRpc]
        public void SetRecordingTime(float time, bool clearFollowingFrames = true) => 
            _ServerReplayController.SetRecordingTime(time, clearFollowingFrames);
    #endregion
        
    #region Playback
        public PlaybackLoadState PlaybackLoadState => _playbackLoadState.Value;
        public bool IsPlaybackLoaded => PlaybackLoadState == PlaybackLoadState.Loaded;
        public bool IsPlaybackLoading => PlaybackLoadState == PlaybackLoadState.Loading;
        public bool IsPlaybackPlaying => _playbackPlaying.Value;
        
        private readonly SyncVar<PlaybackLoadState> _playbackLoadState = new();
        private readonly SyncVar<bool> _playbackPlaying = new();

        [ServerRpc]
        public void StartPlayback() => 
            _ServerReplayController.StartPlayback();

        [ServerRpc]
        public void StopPlayback() => 
            _ServerReplayController.StopPlayback();

        [ServerRpc]
        public void SetPlaybackTime(float newTime) => 
            _ServerReplayController.SetPlaybackTime(newTime);
        
        [ServerRpc]
        public void UnloadPlayback() => 
            _ServerReplayController.UnloadPlayback();

        [ServerRpc]
        public void SaveCurrentRecording() => 
            _ServerReplayController.SaveCurrentRecording();
    #endregion
    }
}
#endif
