// Author: František Holubec
// Created: 29.06.2026

using System;
using EDIVE.Time.DateTimeUtils;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay
{
    [Serializable]
    [MemoryPackable(GenerateType.NoGenerate)]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class AReplayRecordMeta
    {
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("ID")]
        protected string _ID;

        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("Duration")]
        protected float _Duration;
        
        [SerializeField]
        private UDateTime _RecordedAt;

        [MemoryPackIgnore]
        public string ID => _ID;

        [MemoryPackIgnore]
        public float Duration => _Duration;

        [MemoryPackInclude]
        [JsonProperty("RecordedAt")]
        public DateTime RecordedAt
        {
            get => _RecordedAt;
            set => _RecordedAt = value;
        }

        protected AReplayRecordMeta() { }

        protected AReplayRecordMeta(string id, float duration, DateTime recordedAt)
        {
            _ID = id;
            _Duration = duration;
            _RecordedAt = recordedAt;
        }

        public void Stamp(string id, float duration, DateTime recordedAt)
        {
            _ID = id;
            _Duration = duration;
            _RecordedAt = recordedAt;
        }
    }
}
