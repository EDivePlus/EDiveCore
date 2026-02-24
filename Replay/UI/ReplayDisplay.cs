// Author: František Holubec
// Created: 22.07.2025

using System;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Time.TimeSpanUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Replay.UI
{
    public class ReplayDisplay : MonoBehaviour
    {
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

        private ReplayController _replayController;

        private void OnEnable()
        {
            AppCore.Services.SubscribeOnChangeWithInitial<ReplayController>(OnReplayControllerChanged);
        }

        private void OnDisable()
        {
            AppCore.Services.UnsubscribeOnChange<ReplayController>(OnReplayControllerChanged);
        }

        private void OnReplayControllerChanged(ReplayController replayController)
        {
            if (replayController == _replayController)
                return;
            Terminate();
            if (replayController == null) 
                return;
            
            _replayController = replayController;
            Initialize(); 
        }

        private void Initialize()
        {
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

            _replayController.StateChanged += RefreshState;
            _replayController.TimeChanged += RefreshTime;
            RefreshState();
            RefreshTime();
        }

        private void Terminate()
        {
            if (_PlaybackSlider) 
                _PlaybackSlider.onValueChanged.RemoveListener(OnSliderValueChanged);

            if (_PlayPauseButton)
                _PlayPauseButton.onClick.RemoveListener(OnPlayPauseButtonClicked);
            
            if (_RewindButton)
                _RewindButton.onClick.RemoveListener(OnRewindButtonClicked);

            if (_UnloadButton)
                _UnloadButton.onClick.RemoveListener(OnUnloadButtonClicked);
            
            if (_RecordButton)
                _RecordButton.onClick.RemoveListener(OnRecordButtonClicked);
            
            if (_ClearRecordingButton)
                _ClearRecordingButton.onClick.RemoveListener(OnClearRecordingButtonClicked);
            
            if (_SaveButton)
                _SaveButton.onClick.RemoveListener(OnSaveButtonClicked);

            if (_replayController != null)
            {
                _replayController.StateChanged -= RefreshState;
                _replayController.TimeChanged -= RefreshTime;
            }
        }
        
        private void OnSliderValueChanged(float value)
        {
            if (_replayController.IsPlaybackLoaded)
            {
                _replayController.StopPlayback();
                _replayController.SetPlaybackTime(value);
            }
            else if (_replayController.IsPlaybackLoaded)
            {
                _replayController.StopPlayback();
                _replayController.SetPlaybackTime(value);
            }
            else
            {
                _replayController.SetRecordingTime(value, clearFollowingFrames: false);
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
            if (_replayController.IsPlaybackPlaying)
            {
                _replayController.StopPlayback();
            }
            else
            {
                _replayController.StartPlayback();
            }
        }
        
        private void OnRewindButtonClicked()
        {
            _replayController.SetPlaybackTime(0);
        }
        
        private void OnUnloadButtonClicked()
        {
            if (_replayController.IsPlaybackLoaded)
            {
                _replayController.UnloadPlayback();
            }
        }
        
        private void OnSaveButtonClicked()
        {
            // TODO
            // _replayController?.SaveCurrentRecording();
        }

        private void OnClearRecordingButtonClicked()
        {
            _replayController.ResetRecording();
        }

        private void OnRecordButtonClicked()
        {
            if (_replayController.IsRecording)
            {
                _replayController.StopRecording();
            }
            else
            {
                _replayController.StartRecording();
            }
        }
        
        // Todo listed to replay controller state change
        private void RefreshState()
        {
            if (_PlayingState)
                _PlayingState.SetState(_replayController.IsPlaybackPlaying);
            
            if (_RecordingState)
                _RecordingState.SetState(_replayController.IsRecording);
            
            if (_LoadingState)
                _LoadingState.SetState(_replayController.PlaybackLoadState);
            
            if (_HasRecordState)
                _HasRecordState.SetState(_replayController.HasAnyDuration);

            if (_ClearRecordingButton)
                _ClearRecordingButton.interactable = _replayController.HasAnyDuration;
            
            if (_SaveButton)
                _SaveButton.interactable = _replayController.HasAnyDuration;
            
            if (_UnloadButton)
                _UnloadButton.interactable = _replayController.PlaybackLoadState != PlaybackLoadState.NotLoaded;
        }
        
        // Todo listed to replay controller time change
        private void RefreshTime()
        {
            var currentTime = _replayController.CurrentTime;
            var currentDuration = _replayController.CurrentDuration;
            
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
                _HasRecordState.SetState(_replayController.HasAnyDuration);

            if (_ClearRecordingButton)
                _ClearRecordingButton.interactable = _replayController.HasAnyDuration;
            
            if (_SaveButton)
                _SaveButton.interactable = _replayController.HasAnyDuration;
            
            if (_PlayPauseButton)
                _PlayPauseButton.interactable = _replayController.HasAnyDuration;
            
            if (_RewindButton)
                _RewindButton.interactable = _replayController.HasAnyDuration && _replayController.IsPlaybackLoaded;
        }
    }
}
