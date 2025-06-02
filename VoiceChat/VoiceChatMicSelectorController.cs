// Author: František Holubec
// Created: 02.06.2025

using System.Linq;
using Adrenak.UniMic;
using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.VoiceChat
{
    public class VoiceChatMicSelectorController : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown _MicDropdown;

        [SerializeField]
        private Toggle _AllowMicToggle;

        [SerializeField]
        private AToggleState _NoMicToggle;

        private UniVoiceVoiceChatManager _voiceChatManager;

        private void Awake()
        {
            AppCore.Services.WhenRegistered<UniVoiceVoiceChatManager>(Initialize);
        }

        private void Initialize(UniVoiceVoiceChatManager voiceChatManager)
        {
            _voiceChatManager = voiceChatManager;
            var devices = Mic.AvailableDevices;

            if (_MicDropdown)
            {
                _MicDropdown.ClearOptions();
                if (devices.Count > 0)
                {
                    _MicDropdown.AddOptions(devices.Select(d => d.Name).ToList());
                    _MicDropdown.value = voiceChatManager.CurrentMicIndex;
                }
                _MicDropdown.onValueChanged.AddListener(OnMicChanged);
            }

            if (_NoMicToggle)
                _NoMicToggle.SetState(devices.Count == 0);

            if (_AllowMicToggle)
            {
                _AllowMicToggle.isOn = _voiceChatManager.AllowMic;
                _AllowMicToggle.onValueChanged.AddListener(OnAllowMicChanged);
            }
        }

        private void OnAllowMicChanged(bool value)
        {
            _voiceChatManager.AllowMic = value;
        }

        private void OnMicChanged(int value)
        {
            _voiceChatManager.CurrentMicIndex = value;
        }
    }
}
