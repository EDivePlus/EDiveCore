// Author: František Holubec
// Created: 24.02.2026

using System;
using PurrNet.Packing;

namespace EDIVE.Replay.Network
{
    public static class NetworkReplaySerializers
    {
        public static void Write(this BitPacker packer, AReplayRecordMeta value)
        {
            packer.Write(value.ID);
            packer.Write(value.Duration);
            packer.Write(value.RecordedAt.Ticks);
        }

        public static void Read(this BitPacker packer, ref AReplayRecordMeta value)
        {
            string id = null;
            float duration = 0;
            long ticks = 0;
            packer.Read(ref id);
            packer.Read(ref duration);
            packer.Read(ref ticks);
            value = new DefaultReplayRecordMeta(id, duration, new DateTime(ticks));
        }
        
        public static void Write(this BitPacker packer, PlaybackLoadState value)
        {
            packer.Write((byte)value);
        }

        public static void Read(this BitPacker packer, ref PlaybackLoadState value)
        {
            byte raw = 0;
            packer.Read(ref raw);
            value = (PlaybackLoadState)raw;
        }
    }
}
