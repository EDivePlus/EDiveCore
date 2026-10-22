// Author: František Holubec
// Created: 20.10.2026

using System;
using LiveKit;
using LiveKit.Proto;

namespace EDIVE.ServiceHub.Streaming.Sources
{
    // Sends samples handed to it, e.g. the listener output or the voice chat microphone
    public sealed class PushAudioSource : RtcAudioSource
    {
        public override event Action<float[], int, int> AudioRead;

        // Zero rate or channels uses Unity's output format. Null options keep echo cancellation, noise suppression and gain control on
        public PushAudioSource(RtcAudioSourceType sourceType, uint sampleRate = 0, uint channels = 0, AudioSourceOptions nativeOptions = null)
            : base(sourceType, sampleRate, channels, null, nativeOptions) { }

        public void Push(float[] data, int channels, int sampleRate) => AudioRead?.Invoke(data, channels, sampleRate);
    }
}
