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
        private TMP_Dropdown _InputFilterDropdown;

        [SerializeField]
        private TMP_Dropdown _FrameDurationDropdown;

        [SerializeField]
        private Toggle _SpatialAudioToggle;

        private UniVoiceVoiceChatManager _voiceChatManager;

        private static readonly int[] FRAME_DURATIONS = {20, 40, 60};

        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<UniVoiceVoiceChatManager>(Initialize);
        }

        private void Initialize(UniVoiceVoiceChatManager voiceChatManager)
        {
            _voiceChatManager = voiceChatManager;

            if (_FrameDurationDropdown)
            {
                _FrameDurationDropdown.ClearOptions();
                _FrameDurationDropdown.AddOptions(FRAME_DURATIONS.Select(d => $"{d}ms").ToList());
                _FrameDurationDropdown.onValueChanged.RemoveListener(OnFrameDurationChanged);
                OnFrameDurationChanged(FRAME_DURATIONS.IndexOf(_voiceChatManager.MicFrameDurationMS));
                _FrameDurationDropdown.onValueChanged.AddListener(OnFrameDurationChanged);
            }

            if (_InputFilterDropdown)
            {
                _InputFilterDropdown.ClearOptions();
                _InputFilterDropdown.AddOptions(EnumUtils.GetValues<UniVoiceVoiceChatManager.InputFilterType>().Select(d => d.ToString()).ToList());
                _InputFilterDropdown.onValueChanged.RemoveListener(OnInputFilterChanged);
                OnInputFilterChanged((int) _voiceChatManager.InputFilter);
                _InputFilterDropdown.onValueChanged.AddListener(OnInputFilterChanged);
            }

            if (_SpatialAudioToggle)
            {
                _SpatialAudioToggle.onValueChanged.RemoveListener(OnSpatialAudioToggleChanged);
                OnSpatialAudioToggleChanged(_voiceChatManager.EnableSpatialAudio);
                _SpatialAudioToggle.onValueChanged.AddListener(OnSpatialAudioToggleChanged);
            }
        }

        private void OnSpatialAudioToggleChanged(bool value)
        {
            _voiceChatManager.EnableSpatialAudio = value;
        }

        private void OnInputFilterChanged(int value)
        {
            _voiceChatManager.InputFilter = (UniVoiceVoiceChatManager.InputFilterType) value;
        }

        private void OnFrameDurationChanged(int value)
        {
            _voiceChatManager.MicFrameDurationMS = FRAME_DURATIONS[value];
        }
    }
}
