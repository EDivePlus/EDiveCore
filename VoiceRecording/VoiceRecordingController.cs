// Author: František Holubec
// Created: 16.06.2025

using System.Collections;
using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Time.TimeSpanUtils;
using EDIVE.Utils.Activations;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace EDIVE.VoiceRecording
{
    public class VoiceRecordingController : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _ToggleRecordingActivation = new InputActionActivation();

        [SerializeField]
        private Slider _TimerSlider;

        [SerializeField]
        private TimeSpanDisplay _TimerDisplay;
        
        [SerializeField]
        private AToggleState _RecordingState;

        private Coroutine _recordingRoutine;
        private VoiceRecordingManager _voiceRecordingManager;

        private void OnEnable()
        {
            HideRecordingUI();
            _ToggleRecordingActivation?.RegisterActivationListener(ToggleRecording);
        }
        
        private void OnDisable()
        {
            _ToggleRecordingActivation?.UnregisterActivationListener(ToggleRecording);
        }
        
        [Button]
        private void ToggleRecording()
        {
            if (!AppCore.Services.TryGet(out _voiceRecordingManager))
            {
                Debug.LogError("VoiceRecordingManager not found!");
                return;
            }

            if (!_voiceRecordingManager.Recording)
            {
                _voiceRecordingManager.StartRecording();
                ShowRecordingUI();
            }
            else
            {
                _voiceRecordingManager.RecordingStateChanged.RemoveListener(OnVoiceRecordingStateChanged);
                _voiceRecordingManager.StopRecording();
                HideRecordingUI();
            }
        }

        private void OnVoiceRecordingStateChanged(bool state)
        {
            _voiceRecordingManager.RecordingStateChanged.RemoveListener(OnVoiceRecordingStateChanged);
            if (!state)
                HideRecordingUI();
        }
        
        private void ShowRecordingUI()
        {
            if (_RecordingState)
                _RecordingState.SetState(true);
            if (_TimerSlider)
            {
                _TimerSlider.minValue = 0;
                _TimerSlider.maxValue = (int) _voiceRecordingManager.MaxClipDuration.TotalSeconds;
                _TimerSlider.value = 0;
            }

            if (_TimerDisplay)
                _TimerDisplay.SetTimeSpan(System.TimeSpan.Zero);

            if (_recordingRoutine != null) StopCoroutine(_recordingRoutine);
            _recordingRoutine = StartCoroutine(UpdateRecordingUI());
        }
        
        private void HideRecordingUI()
        {
            if (_recordingRoutine != null)
            {
                StopCoroutine(_recordingRoutine);
                _recordingRoutine = null;
            }
            
            if (_RecordingState)
                _RecordingState.SetState(false);
        }

        private IEnumerator UpdateRecordingUI()
        {
            while (_voiceRecordingManager.Recording)
            {
                var currTimespan = _voiceRecordingManager.CurrentRecordingTime;
                
                if (_TimerDisplay)
                    _TimerDisplay.SetTimeSpan(currTimespan);

                if (_TimerSlider) 
                    _TimerSlider.value = (int) currTimespan.TotalSeconds;

                yield return null;
            }
        }
    }
}
