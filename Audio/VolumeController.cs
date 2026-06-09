// Author: František Holubec
// Created: 09.06.2026

using System.Collections.Generic;
using EDIVE.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace EDIVE.Audio
{
    public class VolumeController : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private AudioMixer _Mixer;

        [SerializeField]
        [Required]
        [ValueDropdown("GetExposedParameterOptions")]
        private string _ExposedParameter = "VoiceVolume";

        [SerializeField]
        [Required]
        private Slider _Slider;

        [SerializeField]
        [PropertyRange(0.1f, 10f)]
        [Tooltip("Max slider gain. 1 = 0 dB (unity, no boost). 2 ≈ +6 dB, 10 = +20 dB (mixer ceiling)")]
        private float _MaxValue = 1f;

        private AudioManager _audioManager;

        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<AudioManager>(Initialize);
        }

        private void OnDisable()
        {
            _Slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        private void Initialize(AudioManager audioManager)
        {
            _audioManager = audioManager;
            _Slider.minValue = 0f;
            _Slider.maxValue = _MaxValue;
            _Slider.SetValueWithoutNotify(_audioManager.GetVolume(_ExposedParameter));
            _Slider.onValueChanged.RemoveListener(OnSliderChanged);
            _Slider.onValueChanged.AddListener(OnSliderChanged);
        }

        private void OnSliderChanged(float value)
        {
            if (_audioManager != null)
                _audioManager.SetVolume(_ExposedParameter, value);
        }

#if UNITY_EDITOR
        private IEnumerable<string> GetExposedParameterOptions() => _Mixer.GetExposedParameterOptions();
#endif
    }
}
