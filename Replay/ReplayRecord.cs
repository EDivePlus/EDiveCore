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
        [SerializeField]
        private string _ID;
        
        [MemoryPackInclude]
        [SerializeField]
        private List<ReplayAgentData> _ObjectData;

        [MemoryPackInclude]
        [SerializeField]
        private float _Duration;

        [MemoryPackIgnore]
        public string ID
        {
            get => _ID; 
            set => _ID = value;
        }

        [MemoryPackIgnore]
        public List<ReplayAgentData> ObjectData => _ObjectData;

        [MemoryPackIgnore]
        public float Duration => _Duration;
        
        [MemoryPackConstructor]
        public ReplayRecord() { }
        public ReplayRecord(string id, List<ReplayAgentData> objectData, float duration)
        {
            _ID = id;
            _ObjectData = objectData ?? new List<ReplayAgentData>();
            _Duration = duration;
        }
    }
}
