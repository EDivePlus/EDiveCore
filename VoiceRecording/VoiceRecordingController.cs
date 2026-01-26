// Author: František Holubec
// Created: 16.06.2025

using System.Collections;
using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using TMPro;
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
        private TMP_Text _TimerText;
        
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
            
            _voiceRecordingManager.ToggleVoiceRecording();
            if (_voiceRecordingManager.Recording)
            {
                ShowRecordingUI();
            }
            else
            {
                HideRecordingUI();
            }
        }
        
        private void StopRecording()
        {
            if (!AppCore.Services.TryGet(out _voiceRecordingManager))
            {
                Debug.LogError("VoiceRecordingManager not found!");
                return;
            }
            
            _voiceRecordingManager.StopRecording();
            HideRecordingUI();
        }
        
        private void ShowRecordingUI()
        {
            _RecordingState.SetState(true);
            if (_TimerSlider)
            {
                _TimerSlider.minValue = 0;
                _TimerSlider.maxValue = (int) _voiceRecordingManager.MaxClipDuration.TotalSeconds;
                _TimerSlider.value = 0;
            }

            if (_TimerText)
                _TimerText.text = string.Format("{0:0}:{1:00}", 0, 0);

            if (_recordingRoutine != null) StopCoroutine(_recordingRoutine);
            _recordingRoutine = StartCoroutine(UpdateRecordingUI());
        }
        
        private void HideRecordingUI()
        {
            if (_recordingRoutine != null)
            {
                StopCoroutine(_recordingRoutine);
                _recordingRoutine = null;
                _RecordingState.SetState(false);
            }
        }

        private IEnumerator UpdateRecordingUI()
        {
            while (_voiceRecordingManager.Recording)
            {
                var maxClipDuration = _voiceRecordingManager.MaxClipDuration;
                var currTimespan = _voiceRecordingManager.CurrentRecordingTime;

                if (_TimerText)
                    _TimerText.text = $"{currTimespan.Minutes:0}:{currTimespan.Seconds:00}";

                if (_TimerSlider)
                {
                    if (currTimespan >= maxClipDuration)
                    {
                        _TimerSlider.value = (int) maxClipDuration.TotalSeconds;
                        StopRecording();
                    }
                    else
                        _TimerSlider.value = (int) currTimespan.TotalSeconds;
                }

                yield return null;
            }
        }
    }
}
