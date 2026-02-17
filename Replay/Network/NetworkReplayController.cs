// Author: František Holubec
// Created: 19.08.2025

using EDIVE.External.Signals;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.Replay.Network
{
    [RequireComponent(typeof(ReplayController))]
    public class NetworkReplayController : NetworkBehaviour, IReplayController
    {
    #region Shared
        public Scene Scene => gameObject.scene;
        public float CurrentDuration => Mathf.Max(CurrentTime, _currentDuration.Value);
        public float CurrentTime => _currentTime.Value;
        public bool HasAnyDuration => _currentDuration.Value > 0f;
        
        private readonly SyncVar<float> _currentDuration = new();
        private readonly SyncVar<float> _currentTime = new();
        
        public Signal TimeChanged { get; } = new();
        public Signal StateChanged { get; } = new();
        
        private ReplayController _serverReplayController;

        private void Awake()
        {
            _serverReplayController = GetComponent<ReplayController>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (_serverReplayController == null)
            {
                Debug.LogError("ServerReplayController is not assigned on NetworkReplayController.");
                return;
            }

            _serverReplayController.OverridePlaybackContext(new NetworkPlaybackContext(InstanceFinder.NetworkManager));
            _serverReplayController.StateChanged.AddListener(OnServerStateChanged);
            _serverReplayController.TimeChanged.AddListener(OnServerTimeChanged);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (_serverReplayController == null)
                return;
            
            _serverReplayController.StateChanged.RemoveListener(OnServerStateChanged);
            _serverReplayController.TimeChanged.RemoveListener(OnServerTimeChanged);
        }
        
        [ObserversRpc(RunLocally = true)]
        private void OnServerStateChanged()
        {
            _isRecording.Value = _serverReplayController.IsRecording;
            _playbackLoadState.Value = _serverReplayController.PlaybackLoadState;
            _playbackPlaying.Value = _serverReplayController.IsPlaybackPlaying;
            
            _currentDuration.Value = _serverReplayController.CurrentDuration;
            _currentTime.Value = _serverReplayController.CurrentTime;
            
            StateChanged.Dispatch();
        }

        [ObserversRpc(RunLocally = true)]
        private void OnServerTimeChanged()
        {
            _currentDuration.Value = _serverReplayController.CurrentDuration;
            _currentTime.Value = _serverReplayController.CurrentTime;
            
            TimeChanged.Dispatch();
        }
    #endregion
        
    #region Recording
        public bool IsRecording => _isRecording.Value;
        private readonly SyncVar<bool> _isRecording = new();

        [ServerRpc]
        public void StartRecording() => 
            _serverReplayController.StartRecording();

        [ServerRpc]
        public void StopRecording() => 
            _serverReplayController.StartRecording();

        [ServerRpc]
        public void ResetRecording() => 
            _serverReplayController.ResetRecording();

        [ServerRpc]
        public void SetRecordingTime(float time, bool clearFollowingFrames = true) => 
            _serverReplayController.SetRecordingTime(time, clearFollowingFrames);
        
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
            _serverReplayController.StartPlayback();

        [ServerRpc]
        public void StopPlayback() => 
            _serverReplayController.StopPlayback();

        [ServerRpc]
        public void SetPlaybackTime(float newTime) => 
            _serverReplayController.SetPlaybackTime(newTime);
        
        [ServerRpc]
        public void UnloadPlayback() => 
            _serverReplayController.UnloadPlayback();

        [ServerRpc]
        public void SaveCurrentRecording() => 
            _serverReplayController.SaveCurrentRecording();
    #endregion
    }
}
