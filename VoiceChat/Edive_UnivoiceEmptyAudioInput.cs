using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using System;

public class Edive_UnivoiceEmptyAudioInput : IAudioInput {
    public int Frequency => 1;

    public int ChannelCount => 1;

    public int SegmentRate => 1;

    public event Action<AudioFrame> OnFrameReady;

    public void Dispose() {
        return;
    }
}
