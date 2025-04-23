#if UNIVOICE
using System;
using Adrenak.UniVoice;

namespace EDIVE.MirrorNetworking.VoiceChat
{
    public class UniVoiceEmptyAudioInput : IAudioInput
    {
        public int Frequency => 1;
        public int ChannelCount => 1;
        public int SegmentRate => 1;

        public event Action<AudioFrame> OnFrameReady;
        public void Dispose() { return; }
    }
}
#endif
