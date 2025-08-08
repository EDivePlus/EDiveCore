// Author: František Holubec
// Created: 02.06.2025

using System;
using System.Linq;
using EDIVE.Core;
using EDIVE.NativeUtils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.VoiceChat
{
    public class VoiceChatDebugController : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown _FrameDurationDropdown;

        [SerializeField]
        private Toggle _SpatialAudioToggle;

        private AVoiceChatManager _voiceChatManager;

        private static readonly int[] FRAME_DURATIONS = {20, 40, 60};

        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<AVoiceChatManager>(Initialize);
        }

        private void Initialize(AVoiceChatManager voiceChatManager)
        {
            _voiceChatManager = voiceChatManager;

            if (_FrameDurationDropdown)
            {
                _FrameDurationDropdown.ClearOptions();
                _FrameDurationDropdown.AddOptions(FRAME_DURATIONS.Select(d => $"{d}ms").ToList());
                _FrameDurationDropdown.onValueChanged.RemoveListener(OnFrameDurationChanged);
                _FrameDurationDropdown.value = FRAME_DURATIONS.IndexOf(_voiceChatManager.MicFrameDurationMS);
                _FrameDurationDropdown.onValueChanged.AddListener(OnFrameDurationChanged);
            }

            if (_SpatialAudioToggle)
            {
                _SpatialAudioToggle.onValueChanged.RemoveListener(OnSpatialAudioToggleChanged);
                _SpatialAudioToggle.isOn = _voiceChatManager.EnableSpatialAudio;
                _SpatialAudioToggle.onValueChanged.AddListener(OnSpatialAudioToggleChanged);
            }
        }

        private void OnSpatialAudioToggleChanged(bool value)
        {
            _voiceChatManager.EnableSpatialAudio = value;
        }
        
        private void OnFrameDurationChanged(int value)
        {
            _voiceChatManager.MicFrameDurationMS = FRAME_DURATIONS[value];
        }
    }
}
