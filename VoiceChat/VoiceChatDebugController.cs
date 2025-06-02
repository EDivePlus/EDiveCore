// Author: František Holubec
// Created: 02.06.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.Core;
using EDIVE.NativeUtils;
using TMPro;
using UnityEngine;

namespace EDIVE.VoiceChat
{
    public class VoiceChatDebugController : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown _InputFilterDropdown;

        [SerializeField]
        private TMP_Dropdown _FrameDurationDropdown;

        private UniVoiceVoiceChatManager _voiceChatManager;

        private void Awake()
        {
            AppCore.Services.WhenRegistered<UniVoiceVoiceChatManager>(Initialize);
        }

        private void Initialize(UniVoiceVoiceChatManager voiceChatManager)
        {
            _voiceChatManager = voiceChatManager;

            if (_FrameDurationDropdown)
            {
                _FrameDurationDropdown.ClearOptions();
                _FrameDurationDropdown.AddOptions(new List<string> {"20ms", "40ms", "60ms"});
                _FrameDurationDropdown.value = _voiceChatManager.MicFrameDurationMS / 20 - 1;
                _FrameDurationDropdown.onValueChanged.AddListener(OnFrameDurationChanged);
            }

            if (_InputFilterDropdown)
            {
                _InputFilterDropdown.ClearOptions();
                _InputFilterDropdown.AddOptions(EnumUtils.GetValues<UniVoiceVoiceChatManager.InputFilterType>().Select(d => d.ToString()).ToList());
                _InputFilterDropdown.value = (int) _voiceChatManager.InputFilter;
                _InputFilterDropdown.onValueChanged.AddListener(OnInputFilterChanged);
            }
        }

        private void OnInputFilterChanged(int value)
        {
            _voiceChatManager.InputFilter = (UniVoiceVoiceChatManager.InputFilterType) value;
        }

        private void OnFrameDurationChanged(int value)
        {
            _voiceChatManager.MicFrameDurationMS = (value + 1) * 20;
        }
    }
}
