// Author: František Holubec
// Created: 18.09.2025

using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace EDIVE.EyeTracking
{
    public abstract class AEyeTrackingModule : MonoBehaviour
    {
        public abstract bool IsAvailable { get; }
        public abstract bool IsTracking { get; }
        
        public abstract Observable<EyeGazeFrame> EyeGazeStream { get; }

        public abstract UniTask Initialize();
        public abstract void Terminate();
        
        public abstract void StartTracking(Action<bool> callback = null);
        public abstract void StopTracking();
    }
}
