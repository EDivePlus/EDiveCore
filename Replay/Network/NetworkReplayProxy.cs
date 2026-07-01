// Author: František Holubec
// Created: 19.08.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using PurrNet;
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

        public float CurrentDuration => _currentDuration.value;
        public float CurrentTime => _currentTime.value;

        public event Action TimeChanged;
        public event Action StateChanged;

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer) return;
            _Handler.StateChanged += OnHandlerStateChanged;
            _Handler.TimeChanged += OnHandlerTimeChanged;
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer) return;
            _Handler.StateChanged -= OnHandlerStateChanged;
            _Handler.TimeChanged -= OnHandlerTimeChanged;
        }

        private void OnHandlerStateChanged()
        {
            _isRecording.value = _Handler.IsRecording;
            _playbackLoadState.value = _Handler.PlaybackLoadState;
            _playbackPlaying.value = _Handler.IsPlaybackPlaying;

            _currentDuration.value = _Handler.CurrentDuration;
            _currentTime.value = _Handler.CurrentTime;
            ObserversDispatchStateChanged();
        }

        [ObserversRpc(runLocally: true)]
        private void ObserversDispatchStateChanged() => StateChanged?.Invoke();

        private void OnHandlerTimeChanged()
        {
            _currentDuration.value = _Handler.CurrentDuration;
            _currentTime.value = _Handler.CurrentTime;
            ObserversDispatchTimeChanged();
        }

        [ObserversRpc(runLocally: true)]
        private void ObserversDispatchTimeChanged() => TimeChanged?.Invoke();
    #endregion

    #region Recording
        public bool IsRecording => _isRecording.value;
        private readonly SyncVar<bool> _isRecording = new();

        [ServerRpc(requireOwnership: false)]
        public void StartRecording() => _Handler.StartRecording();

        [ServerRpc(requireOwnership: false)]
        public void StopRecording() => _Handler.StopRecording();

        [ServerRpc(requireOwnership: false)]
        public void ResetRecording() => _Handler.ResetRecording();

        [ServerRpc(requireOwnership: false)]
        public void SetRecordingTime(float time, bool clearFollowingFrames = true) => _Handler.SetRecordingTime(time, clearFollowingFrames);

    #endregion

    #region Playback
        public PlaybackLoadState PlaybackLoadState => _playbackLoadState.value;
        public bool IsPlaybackLoaded => PlaybackLoadState == PlaybackLoadState.Loaded;
        public bool IsPlaybackLoading => PlaybackLoadState == PlaybackLoadState.Loading;
        public bool IsPlaybackPlaying => _playbackPlaying.value;

        private readonly SyncVar<PlaybackLoadState> _playbackLoadState = new();
        private readonly SyncVar<bool> _playbackPlaying = new();

        [ServerRpc(requireOwnership: false)]
        public void StartPlayback() => _Handler.StartPlayback();

        [ServerRpc(requireOwnership: false)]
        public void StopPlayback() => _Handler.StopPlayback();

        [ServerRpc(requireOwnership: false)]
        public void SetPlaybackTime(float newTime) => _Handler.SetPlaybackTime(newTime);

        [ServerRpc(requireOwnership: false)]
        public void UnloadPlayback() => _Handler.UnloadPlayback();

        [ServerRpc(requireOwnership: false)]
        public void SaveCurrentRecording() => _Handler.SaveCurrentRecording();
    #endregion

    #region Saving
        private readonly SyncVar<bool> _isLoadingRecord = new();
        public bool IsLoadingRecord => _isLoadingRecord.value;

        [ServerRpc(requireOwnership: false)]
        public void SaveCurrentRecord() => _Handler.SaveCurrentRecord();

        [ServerRpc(requireOwnership: false)]
        public void LoadRecord(AReplayRecordMeta meta) => _Handler.LoadRecord(meta);

        private UniTaskCompletionSource<IEnumerable<AReplayRecordMeta>> _recordsRequest;
        public async UniTask<IEnumerable<AReplayRecordMeta>> GetSavedRecords()
        {
            if (!isClient)
                return await _Handler.GetSavedRecords();

            _recordsRequest = new UniTaskCompletionSource<IEnumerable<AReplayRecordMeta>>();
            ServerGetSavedRecords();
            var result = await _recordsRequest.Task;
            _recordsRequest = null;
            return result;
        }

        [ServerRpc(requireOwnership: false)]
        private void ServerGetSavedRecords(RPCInfo info = default)
        {
            var sender = info.sender;
            UniTask.Void(async () =>
            {
                var records = await _Handler.GetSavedRecords() ?? Enumerable.Empty<AReplayRecordMeta>();
                TargetReceiveSavedRecords(sender, records.ToList());
            });
        }

        [TargetRpc]
        private void TargetReceiveSavedRecords(PlayerID target, List<AReplayRecordMeta> records)
        {
            _recordsRequest?.TrySetResult(records);
        }
    #endregion
    }
}
