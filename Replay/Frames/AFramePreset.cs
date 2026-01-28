// Author: František Holubec
// Created: 04.07.2025

using System;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Frames
{
    [Serializable]
    [MemoryPackable(GenerateType.NoGenerate)]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class AFramePreset
    {
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("Time")]
        protected float _Time;

        [MemoryPackIgnore]
        public float Time => _Time;

        protected AFramePreset() { }
        protected AFramePreset(float time) => _Time = time;

        public abstract AFramePreset GetCopy();
    }
}
