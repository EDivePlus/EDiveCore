// Author: František Holubec
// Created: 02.06.2025

using EDIVE.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Audio
{
    public class AudioSettingsController : MonoBehaviour
    {
        [SerializeField]
        private Toggle _SpatialAudioToggle;

        private AudioManager _audioManager;
        
        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<AudioManager>(Initialize);
        }

        private void Initialize(AudioManager audioManager)
        {
            _audioManager = audioManager;
            if (_SpatialAudioToggle)
            {
                _SpatialAudioToggle.onValueChanged.RemoveListener(OnSpatialAudioToggleChanged);
                _SpatialAudioToggle.isOn = _audioManager.EnableSpatialAudio;
                _SpatialAudioToggle.onValueChanged.AddListener(OnSpatialAudioToggleChanged);
            }
        }

        private void OnSpatialAudioToggleChanged(bool value)
        {
            _audioManager.EnableSpatialAudio = value;
        }
    }
}
