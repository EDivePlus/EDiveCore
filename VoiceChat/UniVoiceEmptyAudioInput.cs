#if UNIVOICE
using System;
using Adrenak.UniVoice;

namespace EDIVE.VoiceChat
{
    public class UniVoiceEmptyAudioInput : IAudioInput
    {
        public int Frequency => 1;
        public int ChannelCount => 1;
        public int SegmentRate => 1;

#pragma warning disable CS0067
        public event Action<AudioFrame> OnFrameReady;
#pragma warning disable CS0067

        public void Dispose() { return; }
    }
}
#endif
