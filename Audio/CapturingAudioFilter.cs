// Author: František Holubec
// Created: 14.05.2026

using Adrenak.UniVoice;

namespace EDIVE.Audio
{
    public class CapturingAudioFilter : IAudioFilter
    {
        public event System.Action<AudioFrame> AudioFrameCaptured;
        
        public AudioFrame Run(AudioFrame input)
        {
            AudioFrameCaptured?.Invoke(input);
            return input;
        }
    }
}
