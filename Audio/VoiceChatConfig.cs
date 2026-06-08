// Author: František Holubec
// Created: 2026-05-20

using System;
using System.Collections.Generic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Audio
{
    public enum VoiceChatPreset
    {
        Custom,
        NarrowBand8KHz,
        MediumBand12KHzLegacy,
        WideBand16KHz,
        SuperWideBand24KHz,
        FullBand48KHzStandard,
        FullBand48KHzHighFidelity,
    }

    [Serializable]
    public class VoiceChatConfig
    {
        [SerializeField]
        [BoxGroup("Microphone")]
        [LabelText("Sampling Frequency")]
        [ValueDropdown(nameof(GetMicFrequencyOptions))]
        [SuffixLabel("Hz", true)]
        private int _MicSamplingFrequency = 48000;

        [SerializeField]
        [BoxGroup("Microphone")]
        [LabelText("Frame Duration")]
        [ValueDropdown(nameof(GetFrameDurationOptions))]
        [SuffixLabel("ms", true)]
        [Tooltip("Opus supports 2.5/5/10/20/40/60 ms. Lower = less latency, slightly more network overhead. 20 ms is the common choice for real-time voice.")]
        private int _MicFrameDurationMs = 20;

        [SerializeField]
        [BoxGroup("Opus Encoder")]
        [LabelText("Sampling Frequency")]
        [Tooltip("Set to match the mic frequency for best quality (no resampling).")]
        [ValidateInput(nameof(ValidateOpusFrequency),
            "Mic frequency is lower than Opus frequency. Audio will be upsampled before encoding, which adds no real quality. Raise mic or lower Opus to match.",
            InfoMessageType.Warning)]
        private ConcentusFrequencies _OpusFrequency = ConcentusFrequencies.Frequency_48000;

        [SerializeField]
        [BoxGroup("Opus Encoder")]
        [LabelText("Bitrate")]
        [ValueDropdown(nameof(GetBitrateOptions))]
        [Tooltip("64 kbps is the standard for real-time voice. 96 kbps for higher fidelity, 32 kbps for low-bandwidth.")]
        private int _EncoderBitrate = 64000;
        
        [SerializeField]
        [BoxGroup("Opus Encoder")]
        [LabelText("Complexity")]
        [PropertyRange(1, 10)]
        [Tooltip("Higher = better quality at the same bitrate, more CPU. Opus reference recommends 5-10 for voice.")]
        private int _EncoderComplexity = 8;
        
        [SerializeField]
        [BoxGroup("Opus Encoder")]
        [LabelText("Resampler Quality")]
        [PropertyRange(1, 10)]
        [Tooltip("Only used when mic frequency differs from Opus frequency.")]
        private int _ResamplerQuality = 5;

        [SerializeField]
        [BoxGroup("Playback")]
        [LabelText("Target Latency")]
        [ValueDropdown(nameof(GetTargetLatencyOptions))]
        [SuffixLabel("ms", true)]
        [Tooltip("Receiver-side jitter buffer target. Lower = more responsive, more glitches under jitter. Higher = smoother under jitter, more delay.")]
        private int _TargetLatencyMs = 150;

        [SerializeField]
        [BoxGroup("Playback")]
        [LabelText("Pitch Max Correction")]
        [PropertyRange(0f, 0.5f)]
        [Tooltip("Maximum absolute pitch deviation the jitter buffer can apply to catch up or slow down. Package default: 0.2.")]
        private float _PitchMaxCorrection = 0.05f;

        [SerializeField]
        [BoxGroup("Playback")]
        [LabelText("Pitch Proportional Gain")]
        [PropertyRange(0f, 5f)]
        [Tooltip("How aggressively pitch is adjusted per second of latency error. Higher = catches up faster. Package default: 1.0.")]
        private float _PitchProportionalGain = 0.2f;

        [SerializeField]
        [BoxGroup("Playback")]
        [LabelText("Downward Pitch Correction")]
        [PropertyRange(0f, 1f)]
        [Tooltip("Scale for slowing playback (pitch < 1) to let the buffer fill. Package default: 0.25.")]
        private float _DownwardPitchCorrectionScale = 1f;

        [SerializeField]
        [BoxGroup("Filters")]
        [LabelText("RNNoise (Noise Suppression)")]
        [Tooltip("ML-based background noise removal. Can dull sibilants on some mics — try toggling off for A/B comparison.")]
        private bool _UseRnNoise = true;

        [SerializeField]
        [BoxGroup("Filters")]
        [LabelText("Voice Activity Detection")]
        [Tooltip("Drops frames when no speech is detected. Saves bandwidth but can clip word onsets.")]
        private bool _UseSimpleVad = true;

        [SerializeField]
        [BoxGroup("Filters")]
        [ShowIf(nameof(_UseSimpleVad))]
        [LabelText("VAD Attack")]
        [PropertyRange(0, 200)]
        [SuffixLabel("ms", true)]
        [Tooltip("How long speech must persist before the VAD opens. Lower = less onset clipping, but more false triggers from transient noise.")]
        private int _VadAttackMs = 20;

        [SerializeField]
        [BoxGroup("Filters")]
        [ShowIf(nameof(_UseSimpleVad))]
        [LabelText("VAD Release (hangover)")]
        [PropertyRange(100, 2000)]
        [SuffixLabel("ms", true)]
        [Tooltip("How long the VAD keeps transmitting after speech stops. This is the hold-over/hangover: higher values prevent the receiver's jitter buffer from stopping mid-sentence on brief pauses.")]
        private int _VadReleaseMs = 1000;

        [SerializeField]
        [BoxGroup("Filters")]
        [ShowIf(nameof(_UseSimpleVad))]
        [LabelText("VAD Max Gap")]
        [PropertyRange(0, 1000)]
        [SuffixLabel("ms", true)]
        [Tooltip("Quiet gaps up to this long are tolerated without closing while already speaking (keeps short pauses inside a sentence from clipping).")]
        private int _VadMaxGapMs = 300;

        [SerializeField]
        [BoxGroup("Filters")]
        [ShowIf(nameof(_UseSimpleVad))]
        [LabelText("VAD No-Drop Window")]
        [PropertyRange(0, 1000)]
        [SuffixLabel("ms", true)]
        [Tooltip("After opening, the VAD refuses to close for at least this long. Prevents flicker at the very start of an utterance.")]
        private int _VadNoDropWindowMs = 400;

        public int MicSamplingFrequency => _MicSamplingFrequency;
        public int MicFrameDurationMs => _MicFrameDurationMs;
        public ConcentusFrequencies OpusFrequency => _OpusFrequency;
        public int EncoderComplexity => _EncoderComplexity;
        public int EncoderBitrate => _EncoderBitrate;
        public int ResamplerQuality => _ResamplerQuality;
        public bool UseRnNoise => _UseRnNoise;
        public bool UseSimpleVad => _UseSimpleVad;
        public int VadAttackMs => _VadAttackMs;
        public int VadReleaseMs => _VadReleaseMs;
        public int VadMaxGapMs => _VadMaxGapMs;
        public int VadNoDropWindowMs => _VadNoDropWindowMs;
        public float TargetLatencySeconds => _TargetLatencyMs / 1000f;
        public float PitchMaxCorrection => _PitchMaxCorrection;
        public float PitchProportionalGain => _PitchProportionalGain;
        public float DownwardPitchCorrectionScale => _DownwardPitchCorrectionScale;
        
        public SimpleVad.Config BuildVadConfig() => new()
        {
            AttackMs = _VadAttackMs,
            ReleaseMs = _VadReleaseMs,
            MaxGapMs = _VadMaxGapMs,
            NoDropWindowMs = _VadNoDropWindowMs,
        };

        public void ApplyPreset(VoiceChatPreset preset)
        {
            switch (preset)
            {
                case VoiceChatPreset.NarrowBand8KHz:
                    _MicSamplingFrequency = 8000;
                    _OpusFrequency = ConcentusFrequencies.Frequency_8000;
                    _MicFrameDurationMs = 20;
                    _EncoderComplexity = 5;
                    _EncoderBitrate = 24000;
                    _ResamplerQuality = 3;
                    _UseRnNoise = true;
                    _UseSimpleVad = true;
                    break;
                case VoiceChatPreset.MediumBand12KHzLegacy:
                    _MicSamplingFrequency = 12000;
                    _OpusFrequency = ConcentusFrequencies.Frequency_12000;
                    _MicFrameDurationMs = 60;
                    _EncoderComplexity = 3;
                    _EncoderBitrate = 32000;
                    _ResamplerQuality = 2;
                    _UseRnNoise = true;
                    _UseSimpleVad = true;
                    break;
                case VoiceChatPreset.WideBand16KHz:
                    _MicSamplingFrequency = 16000;
                    _OpusFrequency = ConcentusFrequencies.Frequency_16000;
                    _MicFrameDurationMs = 20;
                    _EncoderComplexity = 6;
                    _EncoderBitrate = 32000;
                    _ResamplerQuality = 3;
                    _UseRnNoise = true;
                    _UseSimpleVad = true;
                    break;
                case VoiceChatPreset.SuperWideBand24KHz:
                    _MicSamplingFrequency = 24000;
                    _OpusFrequency = ConcentusFrequencies.Frequency_24000;
                    _MicFrameDurationMs = 20;
                    _EncoderComplexity = 7;
                    _EncoderBitrate = 48000;
                    _ResamplerQuality = 4;
                    _UseRnNoise = true;
                    _UseSimpleVad = true;
                    break;
                case VoiceChatPreset.FullBand48KHzStandard:
                    _MicSamplingFrequency = 48000;
                    _OpusFrequency = ConcentusFrequencies.Frequency_48000;
                    _MicFrameDurationMs = 20;
                    _EncoderComplexity = 8;
                    _EncoderBitrate = 64000;
                    _ResamplerQuality = 5;
                    _UseRnNoise = true;
                    _UseSimpleVad = true;
                    break;
                case VoiceChatPreset.FullBand48KHzHighFidelity:
                    _MicSamplingFrequency = 48000;
                    _OpusFrequency = ConcentusFrequencies.Frequency_48000;
                    _MicFrameDurationMs = 20;
                    _EncoderComplexity = 10;
                    _EncoderBitrate = 96000;
                    _ResamplerQuality = 7;
                    _UseRnNoise = true;
                    _UseSimpleVad = true;
                    break;
                case VoiceChatPreset.Custom:
                default:
                    break;
            }
        }

        private bool ValidateOpusFrequency(ConcentusFrequencies value) =>
            _MicSamplingFrequency >= (int) value;

        private static IEnumerable<ValueDropdownItem<int>> GetMicFrequencyOptions()
        {
            yield return new ValueDropdownItem<int>("8 kHz (Narrow band)", 8000);
            yield return new ValueDropdownItem<int>("12 kHz (Medium band)", 12000);
            yield return new ValueDropdownItem<int>("16 kHz (Wide band)", 16000);
            yield return new ValueDropdownItem<int>("24 kHz (Superwide band)", 24000);
            yield return new ValueDropdownItem<int>("32 kHz", 32000);
            yield return new ValueDropdownItem<int>("44.1 kHz (CD)", 44100);
            yield return new ValueDropdownItem<int>("48 kHz (Full band)", 48000);
        }

        private static IEnumerable<ValueDropdownItem<int>> GetBitrateOptions()
        {
            yield return new ValueDropdownItem<int>("16 kbps (Lowest)", 16000);
            yield return new ValueDropdownItem<int>("24 kbps (Phone)", 24000);
            yield return new ValueDropdownItem<int>("32 kbps (Low)", 32000);
            yield return new ValueDropdownItem<int>("48 kbps", 48000);
            yield return new ValueDropdownItem<int>("64 kbps (Standard)", 64000);
            yield return new ValueDropdownItem<int>("96 kbps (High fidelity)", 96000);
            yield return new ValueDropdownItem<int>("128 kbps (Music)", 128000);
        }

        private static IEnumerable<ValueDropdownItem<int>> GetTargetLatencyOptions()
        {
            yield return new ValueDropdownItem<int>("50 ms (Aggressive)", 50);
            yield return new ValueDropdownItem<int>("100 ms (Low)", 100);
            yield return new ValueDropdownItem<int>("150 ms (Standard)", 150);
            yield return new ValueDropdownItem<int>("250 ms (Safe)", 250);
            yield return new ValueDropdownItem<int>("500 ms (Conservative)", 500);
        }

        private static IEnumerable<ValueDropdownItem<int>> GetFrameDurationOptions()
        {
            yield return new ValueDropdownItem<int>("5 ms (lowest latency)", 5);
            yield return new ValueDropdownItem<int>("10 ms", 10);
            yield return new ValueDropdownItem<int>("20 ms (standard)", 20);
            yield return new ValueDropdownItem<int>("40 ms", 40);
            yield return new ValueDropdownItem<int>("60 ms (most efficient)", 60);
        }
    }
}
