// Author: František Holubec
// Created: 20.10.2026

using System;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.Sources
{
    // Everything the AudioListener plays, on the audio thread
    [RequireComponent(typeof(AudioListener))]
    public class AudioListenerTap : MonoBehaviour
    {
        private int _sampleRate;

        public event Action<float[], int, int> AudioRead;

        private void OnEnable() => _sampleRate = AudioSettings.outputSampleRate;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            AudioRead?.Invoke(data, channels, _sampleRate);
        }
    }
}
