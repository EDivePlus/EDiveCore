// Author: František Holubec
// Created: 11.02.2026

using Adrenak.UniVoice;
using FishNet.CodeGenerating;
using FishNet.Serializing;

namespace EDIVE.Audio
{
    [UseGlobalCustomSerializer]
    public struct NetAudioFrame
    {
        public long Timestamp;
        public int Frequency;
        public int ChannelCount;
        public byte[] Samples;
        
        public NetAudioFrame(AudioFrame value)
        {
            Timestamp = value.timestamp;
            Frequency = value.frequency;
            ChannelCount = value.channelCount;
            Samples = value.samples;
        }

        public static implicit operator AudioFrame(NetAudioFrame wrapper)
        {
            return new AudioFrame
            {
                timestamp = wrapper.Timestamp,
                frequency = wrapper.Frequency,
                channelCount = wrapper.ChannelCount,
                samples = wrapper.Samples
            };
        }

        public static implicit operator NetAudioFrame(AudioFrame value)
        {
            return new NetAudioFrame(value);
        }
    }
    
    public static class NetAudioFrameExtensions
    {
        public static void WriteNetAudioFrame(this Writer writer, NetAudioFrame value)
        {
            writer.WriteInt64(value.Timestamp);
            writer.WriteInt32(value.Frequency);
            writer.WriteInt32(value.ChannelCount);
            writer.WriteUInt8ArrayAndSize(value.Samples);
        }

        public static NetAudioFrame ReadNetAudioFrame(this Reader reader)
        {
            return new NetAudioFrame
            {
                Timestamp = reader.ReadInt64(),
                Frequency = reader.ReadInt32(),
                ChannelCount = reader.ReadInt32(),
                Samples = reader.ReadUInt8ArrayAndSizeAllocated()
            };
        }
    }
}
