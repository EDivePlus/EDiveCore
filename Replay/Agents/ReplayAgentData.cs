// Author: František Holubec
// Created: 21.07.2025

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using MemoryPack;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay
{
    [Serializable]
    [MemoryPackable]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class ReplayAgentData
    {
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("ID")]
        private string _ID;
        
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("SpawnMode")]
        private ReplaySpawnMode _SpawnMode = ReplaySpawnMode.FindOrCreate;

        [HideIf(nameof(_SpawnMode), ReplaySpawnMode.FindOnly)]
        [SerializeField]
        [MemoryPackInclude]
        [MemoryPackAllowSerialize]
        [JsonProperty("Definition")]
        private ReplayAgentDefinition _Definition;
        
        [PropertySpace(4)]
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("TargetTracks")]
        private List<ReplayAgentComponentData> _TargetTracks;

        [MemoryPackIgnore]
        public string ID => _ID;
        [MemoryPackIgnore]
        public ReplaySpawnMode SpawnMode => _SpawnMode;
        [MemoryPackIgnore]
        public ReplayAgentDefinition Definition => _Definition;
        [MemoryPackIgnore]
        public List<ReplayAgentComponentData> TargetTracks => _TargetTracks;
        
        [MemoryPackConstructor]
        [JsonConstructor]
        public ReplayAgentData() { }
        public ReplayAgentData(string id, ReplaySpawnMode spawnMode, ReplayAgentDefinition definition, List<ReplayAgentComponentData> targetTracks)
        {
            _ID = id;
            _SpawnMode = spawnMode;
            _Definition = definition;
            _TargetTracks = targetTracks ?? new List<ReplayAgentComponentData>();
        }
        
        public ReplayAgentComponentData GetTrackData(string id)
        {
            return TargetTracks.TryGetFirst(t => t.ID == id, out var trackData) ? trackData : null;
        }
    }
}
