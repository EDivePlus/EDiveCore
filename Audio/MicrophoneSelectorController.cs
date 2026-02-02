// Author: František Holubec
// Created: 02.06.2025

using System.Collections.Generic;
using EDIVE.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Audio
{
    public class MicrophoneSelectorController : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown _MicDropdown;

        [SerializeField]
        private Toggle _AllowMicToggle;
        
        private AudioManager _audioManager;
        private List<string> _microphones;

        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<AudioManager>(Initialize);
        }

        private void Initialize(AudioManager audioManager)
        {
            _audioManager = audioManager;
            _microphones = _audioManager.GetAvailableMicrophones();
            _microphones.Insert(0, "None");
            var currentIndex = Mathf.Max(0, _microphones.IndexOf(_audioManager.CurrentMicrophoneName));
            
            if (_MicDropdown)
            {
                _MicDropdown.ClearOptions();
                if (_microphones.Count > 0)
                {
                    _MicDropdown.AddOptions(_microphones);
                    _MicDropdown.value = currentIndex;
                    _MicDropdown.onValueChanged.AddListener(OnMicChanged);
                }
            }

            if (_AllowMicToggle)
            {
                _AllowMicToggle.onValueChanged.AddListener(OnAllowMicChanged);
                _AllowMicToggle.isOn = _audioManager.AllowMic;
            }
        }

        private void OnAllowMicChanged(bool value)
        {
            _audioManager.AllowMic = value;
        }

        private void OnMicChanged(int value)
        {
            var micName = value == 0 ? null : _microphones[value];
            _audioManager.TrySetMicrophone(micName);
        }
    }
}
