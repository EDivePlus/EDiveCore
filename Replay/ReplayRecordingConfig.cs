// Author: František Holubec
// Created: 07.07.2025

using System;
using EDIVE.Utils.Cysharp;
using UnityEngine;

namespace EDIVE.Replay
{
    [Serializable]
    public class ReplayRecordingConfig : ScriptableObject
    {
        [SerializeField]
        private TimingPreset _Timing;

        public TimingPreset Timing => _Timing;
    }
}
