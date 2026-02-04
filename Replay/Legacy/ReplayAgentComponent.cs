// Author: František Holubec
// Created: 04.07.2025

using System;
using System.Collections.Generic;
using EDIVE.Replay.Frames;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Replay
{
    [Serializable]
    public class ReplayAgentComponent
    {
        [SerializeField]
        private Object _Target;
        
        [SerializeField]
        private string _ID;
        
        [SerializeReference]
        private List<AFrameSequence> _FrameSequences = new();

        public string ID => _ID;
        public Object Target { get => _Target; set => _Target = value; }
        public List<AFrameSequence> FrameSequences => _FrameSequences;
    }
}
