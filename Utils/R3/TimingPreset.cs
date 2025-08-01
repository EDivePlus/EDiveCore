// Author: František Holubec
// Created: 21.07.2025

#if R3
using System;
using MemoryPack;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.R3
{
    [Serializable]
    [InlineProperty]
    [MemoryPackable]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class TimingPreset
    {
        [SuffixLabel("s", true)]
        [HideLabel]
        [HorizontalGroup(0.3f)]
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("TimeStep")]
        private float _TimeStep = 0.2f;
        
        [HideLabel]
        [HorizontalGroup]
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("TimeProvider")]
        private TimeProviderPreset _TimeProvider = new();
        
        [MemoryPackIgnore]
        public TimeProvider TimeProvider => _TimeProvider;
        
        [MemoryPackIgnore]
        public TimeSpan TimeStep => TimeSpan.FromSeconds(_TimeStep);
        
        [MemoryPackIgnore]
        public float TimeStepSeconds => _TimeStep;
    }
}
#endif
