// Author: František Holubec
// Created: 24.02.2026

using FishNet.CodeGenerating;
using FishNet.Serializing;

namespace EDIVE.Replay.Network
{
    [UseGlobalCustomSerializer]
    public struct NetReplayRecordInfo
    {
        public string ID;
        public float Duration;
        
        public NetReplayRecordInfo(ReplayRecordInfo value)
        {
            ID = value.ID;
            Duration = value.Duration;
        }
        
        public static implicit operator ReplayRecordInfo(NetReplayRecordInfo wrapper)
        {
            return new ReplayRecordInfo(wrapper.ID, wrapper.Duration);
        }

        public static implicit operator NetReplayRecordInfo(ReplayRecordInfo value)
        {
            return new NetReplayRecordInfo(value);
        }
    }
    
    public static class NetReplayRecordInfoExtensions
    {
        public static void WriteNetReplayRecordInfo(this Writer writer, NetReplayRecordInfo value)
        {
            writer.WriteString(value.ID);
            writer.WriteSingle(value.Duration);
        }

        public static NetReplayRecordInfo ReadNetReplayRecordInfo(this Reader reader)
        {
            return new NetReplayRecordInfo
            {
                ID = reader.ReadStringAllocated(),
                Duration = reader.ReadSingle()
            };
        }
        
        public static NetReplayRecordInfo ToNetSerialized(this ReplayRecordInfo info) => info;
        public static ReplayRecordInfo FromNetSerialized(this NetReplayRecordInfo info) => info;
    }
}
