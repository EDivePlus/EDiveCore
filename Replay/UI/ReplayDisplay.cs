// Author: František Holubec
// Created: 22.07.2025

using System;
using EDIVE.DataStructures;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Time.TimeSpanUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Replay
{
    public class ReplayDisplay : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private SerializedInterface<IReplayController, MonoBehaviour> _ReplayController;

        [PropertySpace]
        [SerializeField]
        private Slider _PlaybackSlider;
        
        [SerializeField]
        private TimeSpanDisplay _CurrentTimeDisplay;
        
        [SerializeField]
        private TimeSpanDisplay _DurationDisplay;
        
        [SerializeField]
        private Button _PlayPauseButton;
        
        [SerializeField]
        private Button _RewindButton;
        
        [SerializeField]
        private Button _UnloadButton;
        
        [SerializeField]
        private AToggleState _PlayingState;

        [ValidateMultiState(typeof(PlaybackLoadState))]
        [SerializeField]
        private AMultiState _LoadingState;
        
        [PropertySpace]
        [SerializeField]
        private Button _RecordButton;
        
        [SerializeField]
        private Button _ClearRecordingButton;
        
        [SerializeField]
        private Button _SaveButton;
        
        [SerializeField]
        private AToggleState _RecordingState;
        
        [SerializeField]
        private AToggleState _HasRecordState;
        
        private IReplayController ReplayController => _ReplayController.Value;
   
        private void Awake()
        {
            if (_ReplayController == null)
                return;
            
            if (_PlaybackSlider)
            {
                _PlaybackSlider.minValue = 0;
                _PlaybackSlider.maxValue = 1;
                _PlaybackSlider.value = 0;
                _PlaybackSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (_PlayPauseButton)
                _PlayPauseButton.onClick.AddListener(OnPlayPauseButtonClicked);
            
            if (_RewindButton)
                _RewindButton.onClick.AddListener(OnRewindButtonClicked);

            if (_UnloadButton)
                _UnloadButton.onClick.AddListener(OnUnloadButtonClicked);
            
            if (_RecordButton)
                _RecordButton.onClick.AddListener(OnRecordButtonClicked);
            
            if (_ClearRecordingButton)
                _ClearRecordingButton.onClick.AddListener(OnClearRecordingButtonClicked);
            
            if (_SaveButton)
                _SaveButton.onClick.AddListener(OnSaveButtonClicked);

            ReplayController.StateChanged.AddListener(RefreshState);
            ReplayController.TimeChanged.AddListener(RefreshTime);
            RefreshState();
            RefreshTime();
        }
        
        private void OnSliderValueChanged(float value)
        {
            if (ReplayController.IsPlaybackLoaded)
            {
                ReplayController.StopPlayback();
                ReplayController.SetPlaybackTime(value);
            }
            else if (ReplayController.IsPlaybackLoaded)
            {
                ReplayController.StopPlayback();
                ReplayController.SetPlaybackTime(value);
            }
            else
            {
                ReplayController.SetRecordingTime(value, clearFollowingFrames: false);
            }
        }
        
        private void OnDestroy()
        {
            if (_PlaybackSlider) 
                _PlaybackSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            if (_PlayPauseButton) 
                _PlayPauseButton.onClick.RemoveListener(OnPlayPauseButtonClicked);
        }
        
        private void OnPlayPauseButtonClicked()
        {
            if (ReplayController.IsPlaybackPlaying)
            {
                ReplayController.StopPlayback();
            }
            else
            {
                ReplayController.StartPlayback();
            }
        }
        
        private void OnRewindButtonClicked()
        {
            ReplayController.SetPlaybackTime(0);
        }
        
        private void OnUnloadButtonClicked()
        {
            if (ReplayController.IsPlaybackLoaded)
            {
                ReplayController.UnloadPlayback();
            }
        }
        
        private void OnSaveButtonClicked()
        {
            // TODO implement save functionality
            // https://github.com/yasirkula/UnityNativeFilePicker maybe? already imported in the project
            
                if (_ReplayController != null)
                    ReplayController.SaveCurrentRecording();
        }

        private void OnClearRecordingButtonClicked()
        {
            ReplayController.ResetRecording();
        }

        private void OnRecordButtonClicked()
        {
            if (ReplayController.IsRecording)
            {
                ReplayController.StopRecording();
            }
            else
            {
                ReplayController.StartRecording();
            }
        }
        
        // Todo listed to replay controller state change
        private void RefreshState()
        {
            if (_PlayingState)
                _PlayingState.SetState(ReplayController.IsPlaybackPlaying);
            
            if (_RecordingState)
                _RecordingState.SetState(ReplayController.IsRecording);
            
            if (_LoadingState)
                _LoadingState.SetState(ReplayController.PlaybackLoadState);
            
            if (_HasRecordState)
                _HasRecordState.SetState(ReplayController.HasAnyDuration);

            if (_ClearRecordingButton)
                _ClearRecordingButton.interactable = ReplayController.HasAnyDuration;
            
            if (_SaveButton)
                _SaveButton.interactable = ReplayController.HasAnyDuration;
            
            if (_UnloadButton)
                _UnloadButton.interactable = ReplayController.PlaybackLoadState != PlaybackLoadState.NotLoaded;
        }
        
        // Todo listed to replay controller time change
        private void RefreshTime()
        {
            var currentTime = ReplayController.CurrentTime;
            var currentDuration = ReplayController.CurrentDuration;
            
            if (_CurrentTimeDisplay)
                _CurrentTimeDisplay.SetTimeSpan(TimeSpan.FromSeconds(currentTime));

            if (_DurationDisplay)
                _DurationDisplay.SetTimeSpan(TimeSpan.FromSeconds(currentDuration));

            if (_PlaybackSlider)
            {
                _PlaybackSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
                _PlaybackSlider.maxValue = currentDuration;
                _PlaybackSlider.value = currentTime;
                _PlaybackSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
            
            if (_HasRecordState)
                _HasRecordState.SetState(ReplayController.HasAnyDuration);

            if (_ClearRecordingButton)
                _ClearRecordingButton.interactable = ReplayController.HasAnyDuration;
            
            if (_SaveButton)
                _SaveButton.interactable = ReplayController.HasAnyDuration;
            
            if (_PlayPauseButton)
                _PlayPauseButton.interactable = ReplayController.HasAnyDuration;
            
            if (_RewindButton)
                _RewindButton.interactable = ReplayController.HasAnyDuration && ReplayController.IsPlaybackLoaded;
        }
    }
}
