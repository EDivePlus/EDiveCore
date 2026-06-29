// Author: František Holubec
// Created: 29.06.2026

using System;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;

namespace EDIVE.Replay
{
    [Serializable]
    [MemoryPackable]
    [MemoryPackUnionTag(0)]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class DefaultReplayRecordMeta : AReplayRecordMeta
    {
        [MemoryPackConstructor]
        public DefaultReplayRecordMeta() { }

        public DefaultReplayRecordMeta(string id, float duration, DateTime recordedAt = default)
            : base(id, duration, recordedAt) { }
    }
}
