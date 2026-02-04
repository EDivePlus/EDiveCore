// Author: František Holubec
// Created: 21.07.2025

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.Replay.Components;
using MemoryPack;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay.Agents
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
        [SerializeReference]
        [MemoryPackInclude]
        [JsonProperty("ComponentData")]
        private List<AReplayAgentComponentData> _ComponentData;

        [MemoryPackIgnore]
        public string ID => _ID;
        [MemoryPackIgnore]
        public ReplaySpawnMode SpawnMode => _SpawnMode;
        [MemoryPackIgnore]
        public ReplayAgentDefinition Definition => _Definition;
        [MemoryPackIgnore]
        public List<AReplayAgentComponentData> ComponentData => _ComponentData;
        
        [MemoryPackConstructor]
        [JsonConstructor]
        public ReplayAgentData() { }
        public ReplayAgentData(string id, ReplaySpawnMode spawnMode, ReplayAgentDefinition definition, List<AReplayAgentComponentData> componentData)
        {
            _ID = id;
            _SpawnMode = spawnMode;
            _Definition = definition;
            _ComponentData = componentData ?? new List<AReplayAgentComponentData>();
        }
        
        public AReplayAgentComponentData GetComponentData(string id)
        {
            return _ComponentData.TryGetFirst(t => t.ID == id, out var componentData) ? componentData : null;
        }
    }
}
