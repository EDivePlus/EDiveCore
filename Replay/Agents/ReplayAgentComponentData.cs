// Author: František Holubec
// Created: 21.07.2025

using System;
using System.Collections.Generic;
using EDIVE.Replay.Frames;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay
{
    [Serializable]
    [MemoryPackable]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class ReplayAgentComponentData
    {
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("ID")]
        private string _ID;
        
        [SerializeReference]
        [MemoryPackInclude]
        [JsonProperty("FrameSequences")]
        private List<AFrameSequence> _FrameSequences = new();

        [MemoryPackIgnore]
        public string ID => _ID;
        [MemoryPackIgnore]
        public List<AFrameSequence> FrameSequences => _FrameSequences;

        [MemoryPackConstructor]
        [JsonConstructor]
        public ReplayAgentComponentData() { }

        public ReplayAgentComponentData(string id, List<AFrameSequence> frameSequences)
        {
            _ID = id;
            _FrameSequences = frameSequences ?? new List<AFrameSequence>();
        }
    }
}
