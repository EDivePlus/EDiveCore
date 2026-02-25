// Author: František Holubec
// Created: 19.08.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay.Network
{
    public class NetworkReplayProxy : NetworkBehaviour
    {
    #region Shared
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private ReplayHandler<NetworkReplayAgentSpawnStrategy> _Handler;
        
        private readonly SyncVar<float> _currentDuration = new();
        private readonly SyncVar<float> _currentTime = new();
        
        public float CurrentDuration => _currentDuration.Value;
        public float CurrentTime => _currentTime.Value;

        public event Action TimeChanged;
        public event Action StateChanged;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            _Handler.StateChanged += OnHandlerStateChanged;
            _Handler.TimeChanged += OnHandlerTimeChanged;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _Handler.StateChanged -= OnHandlerStateChanged;
            _Handler.TimeChanged += OnHandlerTimeChanged;
        }
        
        private void OnHandlerStateChanged()
        {
            _isRecording.Value = _Handler.IsRecording;
            _playbackLoadState.Value = _Handler.PlaybackLoadState;
            _playbackPlaying.Value = _Handler.IsPlaybackPlaying;

            _currentDuration.Value = _Handler.CurrentDuration;
            _currentTime.Value = _Handler.CurrentTime;
            ObserversDispatchStateChanged();
        }

        [ObserversRpc(RunLocally = true)]
        private void ObserversDispatchStateChanged() => StateChanged?.Invoke();

        private void OnHandlerTimeChanged()
        {
            _currentDuration.Value = _Handler.CurrentDuration;
            _currentTime.Value = _Handler.CurrentTime;
            ObserversDispatchTimeChanged();
        }
        
        [ObserversRpc(RunLocally = true)]
        private void ObserversDispatchTimeChanged() => TimeChanged?.Invoke();
    #endregion

    #region Recording
        public bool IsRecording => _isRecording.Value;
        private readonly SyncVar<bool> _isRecording = new();

        [ServerRpc(RequireOwnership = false)]
        public void StartRecording() => _Handler.StartRecording();

        [ServerRpc(RequireOwnership = false)]
        public void StopRecording() => _Handler.StopRecording();

        [ServerRpc(RequireOwnership = false)]
        public void ResetRecording() => _Handler.ResetRecording();

        [ServerRpc(RequireOwnership = false)]
        public void SetRecordingTime(float time, bool clearFollowingFrames = true) => _Handler.SetRecordingTime(time, clearFollowingFrames);
        
    #endregion
        
    #region Playback
        public PlaybackLoadState PlaybackLoadState => _playbackLoadState.Value;
        public bool IsPlaybackLoaded => PlaybackLoadState == PlaybackLoadState.Loaded;
        public bool IsPlaybackLoading => PlaybackLoadState == PlaybackLoadState.Loading;
        public bool IsPlaybackPlaying => _playbackPlaying.Value;
        
        private readonly SyncVar<PlaybackLoadState> _playbackLoadState = new();
        private readonly SyncVar<bool> _playbackPlaying = new();

        [ServerRpc(RequireOwnership = false)]
        public void StartPlayback() => _Handler.StartPlayback();

        [ServerRpc(RequireOwnership = false)]
        public void StopPlayback() => _Handler.StopPlayback();
        
        [ServerRpc(RequireOwnership = false)]
        public void SetPlaybackTime(float newTime) => _Handler.SetPlaybackTime(newTime);
        
        [ServerRpc(RequireOwnership = false)]
        public void UnloadPlayback() => _Handler.UnloadPlayback();

        [ServerRpc(RequireOwnership = false)]
        public void SaveCurrentRecording() => _Handler.SaveCurrentRecording();
    #endregion

    #region Saving
        private readonly SyncVar<bool> _isLoadingRecord = new();
        public bool IsLoadingRecord => _isLoadingRecord.Value;
        
        [ServerRpc(RequireOwnership = false)]
        public void SaveCurrentRecord() => _Handler.SaveCurrentRecord();
        
        [ServerRpc(RequireOwnership = false)]
        public void LoadRecord(ReplayRecordInfo info) => _Handler.LoadRecord(info);
        
        private UniTaskCompletionSource<IEnumerable<ReplayRecordInfo>> _recordsRequest;
        public async UniTask<IEnumerable<ReplayRecordInfo>> GetSavedRecords()
        {
            if (!IsClientInitialized)
                return await _Handler.GetSavedRecords();
            
            _recordsRequest = new UniTaskCompletionSource<IEnumerable<ReplayRecordInfo>>();
            ServerGetSavedRecords();
            var result = await _recordsRequest.Task;
            _recordsRequest = null;
            return result;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void ServerGetSavedRecords(NetworkConnection conn = null)
        {
            UniTask.Void(async () =>
            {
                var records = await _Handler.GetSavedRecords() ?? Enumerable.Empty<ReplayRecordInfo>();
                TargetReceiveSavedRecords(conn, records.Select(r => r.ToNetSerialized()).ToList());
            });
        }
        
        [TargetRpc]
        private void TargetReceiveSavedRecords(NetworkConnection conn, List<NetReplayRecordInfo> records)
        {
            _recordsRequest?.TrySetResult(records.Select(r => r.FromNetSerialized()));
        }
    #endregion
    }
}
