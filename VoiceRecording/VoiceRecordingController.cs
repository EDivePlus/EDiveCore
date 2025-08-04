// Author: František Holubec
// Created: 16.06.2025

using System.Collections;
using EDIVE.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace EDIVE.VoiceRecording
{
    public class VoiceRecordingController : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference _ToggleRecordingAction;

        // Extensibility: we may want to have voice recording triggered by a UI element in the future
        //[SerializeField]
        //private Button _ToggleRecordingButton;

        [SerializeField]
        private Slider _TimerSlider;

        [SerializeField]
        private TextMeshProUGUI _TimerText;

        [SerializeField]
        private TextMeshProUGUI _RecordingOnText;

        [SerializeField]
        private Transform _RecordingDescriptions;

        private Coroutine _recordingRoutine;
        private VoiceRecordingManager _voiceRecordingManager;
        
        [Tooltip("Used to show that this is the controller instance that initiated the current ongoing recording.")]
        [ShowInInspector]
        public bool IsRecording;

        private void OnEnable()
        {
            HideRecordingUI();
            AppCore.Services.WhenRegistered<VoiceRecordingManager>(Initialize);
        }

        private void Initialize(VoiceRecordingManager voiceRecordingManager)
        {
            _voiceRecordingManager = voiceRecordingManager;

            if (_ToggleRecordingAction)
                _ToggleRecordingAction.action.performed += RecordingActionPerformed;

            // Extensibility: we may want to have voice recording triggered by a UI element in the future
            //if (_ToggleRecordingButton)
            //    _ToggleRecordingButton.onClick.AddListener(ToggleRecording);

        }

        private void RecordingActionPerformed(InputAction.CallbackContext ctx)
        {
            ToggleRecording();
        }

        [Button]
        private void ToggleRecording()
        {
            Debug.Log(gameObject.name);
            Debug.Log("VoiceRecordingController.ToggleRecording entered.");
            _voiceRecordingManager.ToggleVoiceRecording(this);
            Debug.Log("_voiceRecordingManager.ToggleVoiceRecording() called.");
            if (_voiceRecordingManager.Recording && IsRecording)
            {
                Debug.Log("Recording is ON. Showing UI...");
                ShowRecordingUI();
            }
            else
            {
                Debug.Log("Recording is OFF. Hiding UI...");
                HideRecordingUI();
            }
        }

        private void OnDisable()
        {
            if (_ToggleRecordingAction)
                _ToggleRecordingAction.action.performed -= RecordingActionPerformed;

            // Extensibility: we may want to have voice recording triggered by a UI element in the future
            //if (_ToggleRecordingButton)
            //    _ToggleRecordingButton.onClick.RemoveListener(ToggleRecording);
        }
        
        private void ShowRecordingUI()
        {
            _RecordingDescriptions.gameObject.SetActive(true);
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
                    // the recording should be stopped with voiceRecording = false
                    if (currTimespan >= maxClipDuration)
                    {
                        _TimerSlider.value = (int) maxClipDuration.TotalSeconds;
                        ToggleRecording();
                    }
                    else
                        _TimerSlider.value = (int) currTimespan.TotalSeconds;
                }

                yield return null;
            }
        }

        private void HideRecordingUI()
        {
            if (_recordingRoutine != null)
            {
                StopCoroutine(_recordingRoutine);
                _recordingRoutine = null;
                _RecordingDescriptions.gameObject.SetActive(false);
            }
        }
    }
}
