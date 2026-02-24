// Author: František Holubec
// Created: 24.02.2026

using System;
using UnityEngine;

namespace EDIVE.Replay
{
    [Serializable]
    public struct ReplayRecordInfo
    {
        [SerializeField]
        private string _ID;
        
        [SerializeField]
        private float _Duration;

        public string ID => _ID;
        public float Duration => _Duration;
        
        public ReplayRecordInfo(string id, float duration)
        {
            _ID = id;
            _Duration = duration;
        }
    }
}
