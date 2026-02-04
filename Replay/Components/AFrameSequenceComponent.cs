// Author: František Holubec
// Created: 04.02.2026

using System;
using System.Threading;

namespace EDIVE.Replay.Components
{
    [Serializable]
    public abstract class AFrameSequenceComponent<TTarget, TData> : AReplayAgentComponent<TTarget, TData>
        where TData : AFrameSequenceComponentData<TTarget>, new()
        where TTarget : UnityEngine.Object
    {
        protected AFrameSequenceComponent() { }
        protected AFrameSequenceComponent(TTarget target, TData data) : base(target, data) { }

        public override void StartRecording(float startTime, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
        {
            if (_Data != null && _Target != null) 
                _Data?.StartRecording(startTime, _Target, config, cancellationToken);
        }

        public override void StartPlayback(float startTime, CancellationToken cancellationToken = default)
        {
            if (_Data != null && _Target != null) 
                _Data?.StartPlayback(startTime, _Target, cancellationToken);
        }

        public override void ApplyTime(float time)
        {
            if (_Data != null && _Target != null) 
                _Data.ApplyTime(_Target, time);
        }

        public override void ClearData(float startTime = 0)
        {
            _Data?.Clear(f => f.Time >= startTime);
        }
    }
}
