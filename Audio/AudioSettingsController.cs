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
        private Toggle _VoiceChatToggle;

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

            if (_VoiceChatToggle)
            {
                _VoiceChatToggle.onValueChanged.RemoveListener(OnVoiceChatToggleChanged);
                _VoiceChatToggle.isOn = _audioManager.EnableVoiceChat;
                _VoiceChatToggle.onValueChanged.AddListener(OnVoiceChatToggleChanged);
            }
        }

        private void OnSpatialAudioToggleChanged(bool value)
        {
            _audioManager.EnableSpatialAudio = value;
        }

        private void OnVoiceChatToggleChanged(bool value)
        {
            _audioManager.EnableVoiceChat = value;
        }
    }
}
