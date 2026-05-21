// Author: František Holubec
// Created: 24.02.2026

using PurrNet.Packing;

namespace EDIVE.Replay.Network
{
    public static class NetworkReplaySerializers
    {
        public static void Write(this BitPacker packer, ReplayRecordInfo value)
        {
            packer.Write(value.ID);
            packer.Write(value.Duration);
        }
        
        public static void Read(this BitPacker packer, ref ReplayRecordInfo value)
        {
            string id = null;
            float duration = 0;
            packer.Read(ref id);
            packer.Read(ref duration);
            value = new ReplayRecordInfo(id, duration);
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
