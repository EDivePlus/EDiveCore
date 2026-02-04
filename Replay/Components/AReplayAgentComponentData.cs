// Author: František Holubec
// Created: 04.02.2026

using System;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using MemoryPack;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.Replay.Components
{
    [Serializable]
    [MemoryPackable(GenerateType.NoGenerate)]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class AReplayAgentComponentData
    {
        [Required]
        [InlineIconButton("Refresh", "RefreshID", "Refresh ID")]
        [JsonProperty("ID")]
        [MemoryPackInclude]
        [SerializeField]
        protected string _ID;

        [MemoryPackIgnore]
        public string ID => _ID;
        
        protected AReplayAgentComponentData() { }
        protected AReplayAgentComponentData(string id)
        {
            _ID = id;
        }
        
        public abstract float GetMinTime();
        public abstract float GetMaxTime();
        public abstract AReplayAgentComponentData GetCopy();
        
#if UNITY_EDITOR
        protected virtual void RefreshID(InspectorProperty property)
        {
            if (!property.TryGetParentObject<AReplayAgentComponent>(out var agentComponent)) 
                return;
            
            _ID = agentComponent.GenerateID(property);
            property.MarkSerializationRootDirty();
        }
#endif
    }
}
