// Author: Radim Holub
// Created: 15.07.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Core.Services;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay
{
    public class ReplayController : AServiceBehaviour<ReplayController>
    {
        [SerializeField] 
        private List<ReplayHandlerWrapper> _AvailableHandlers;

        public IReplayHandler CurrentHandler { get; private set; }

        [Serializable]
        public class ReplayHandlerWrapper
        {
            [SerializeField]
            private string _ID;
            
            [HideLabel]
            [InlineProperty]
            [SerializeReference] 
            private IReplayHandler _Handler;
            
            public string ID => _ID;
            public IReplayHandler Handler => _Handler;
        }
        
        public bool TrySetHandler(string handlerID)
        {
            if (string.IsNullOrEmpty(handlerID) || !_AvailableHandlers.TryGetFirst(h => h.Handler != null && h.ID == handlerID, out var wrapper))
                return false;

            return TrySetHandler(wrapper.Handler);
        }
        
        private bool TrySetHandler(IReplayHandler handler)
        {
            if (handler == null || handler == CurrentHandler)
                return false;
            
            if (CurrentHandler != null)
            {
                CurrentHandler.Terminate();
                CurrentHandler.StateChanged -= StateChanged;
                CurrentHandler.TimeChanged -= TimeChanged;
            }
               
            CurrentHandler = handler;
            CurrentHandler.Initialize();
            CurrentHandler.StateChanged += StateChanged;
            CurrentHandler.TimeChanged += TimeChanged;
            return true;
        }
        
        private void Awake()
        {
            TrySetHandler(_AvailableHandlers.FirstOrDefault(h => h.Handler != null)?.Handler);
        }

        private void OnDestroy()
        {
            CurrentHandler?.Terminate();
        }
        
        public event Action TimeChanged;
        public event Action StateChanged;

        public float CurrentDuration => CurrentHandler?.CurrentDuration ?? 0;
        public bool HasAnyDuration => CurrentHandler?.HasAnyDuration ?? false;
        public float CurrentTime => CurrentHandler?.CurrentTime ?? 0;
        public bool IsRecording => CurrentHandler?.IsRecording ?? false;
        
        public void StartRecording() => CurrentHandler?.StartRecording();
        public void StopRecording() => CurrentHandler?.StopRecording();
        public void ResetRecording() => CurrentHandler?.ResetRecording();
        public void SetRecordingTime(float time, bool clearFollowingFrames = true) => CurrentHandler?.SetRecordingTime(time, clearFollowingFrames);

        public PlaybackLoadState PlaybackLoadState => CurrentHandler?.PlaybackLoadState ?? PlaybackLoadState.NotLoaded;
        public bool IsPlaybackLoaded => CurrentHandler?.IsPlaybackLoaded ?? false;
        public bool IsPlaybackLoading => CurrentHandler?.IsPlaybackLoaded ?? false;
        public bool IsPlaybackPlaying => CurrentHandler?.IsPlaybackPlaying ?? false;

        public void StartPlayback() => CurrentHandler?.StartPlayback();
        public void StopPlayback() => CurrentHandler?.StopPlayback();
        public void SetPlaybackTime(float newTime) => CurrentHandler?.SetPlaybackTime(newTime);
        public void UnloadPlayback() => CurrentHandler?.UnloadPlayback();
                
        public void SaveCurrentRecord() => CurrentHandler?.SaveCurrentRecord();
        public void LoadRecord(ReplayRecordInfo info) => CurrentHandler?.LoadRecord(info);
        public async UniTask<IEnumerable<ReplayRecordInfo>> GetSavedRecords() => CurrentHandler != null ? await CurrentHandler.GetSavedRecords() : Enumerable.Empty<ReplayRecordInfo>();
    }
}
