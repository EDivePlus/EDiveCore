// Author: František Holubec
// Created: 21.07.2025

using System;
using System.Collections.Generic;
using EDIVE.Replay.Agents;
using MemoryPack;
using UnityEngine;

namespace EDIVE.Replay
{
    [MemoryPackable]
    [Serializable]
    public partial class ReplayRecord
    {
        [MemoryPackInclude]
        [MemoryPackAllowSerialize]
        [SerializeReference]
        private AReplayRecordMeta _Meta;

        [MemoryPackInclude]
        [SerializeField]
        private List<ReplayAgentData> _ObjectData;

        [MemoryPackIgnore]
        public AReplayRecordMeta Meta => _Meta;

        [MemoryPackIgnore]
        public string ID => _Meta?.ID;
        
        [MemoryPackIgnore]
        public float Duration => _Meta?.Duration ?? 0f;

        [MemoryPackIgnore]
        public List<ReplayAgentData> ObjectData => _ObjectData;
        
        [MemoryPackConstructor]
        public ReplayRecord() { }
        public ReplayRecord(AReplayRecordMeta meta, List<ReplayAgentData> objectData)
        {
            _Meta = meta;
            _ObjectData = objectData ?? new List<ReplayAgentData>();
        }
    }
}
