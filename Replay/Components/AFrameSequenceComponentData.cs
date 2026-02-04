// Author: František Holubec
// Created: 04.02.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MemoryPack;
using Newtonsoft.Json;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay.Components
{
    [Serializable]
    [MemoryPackable(GenerateType.NoGenerate)]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class AFrameSequenceComponentData<TTarget> : AReplayAgentComponentData
    {
        [MemoryPackIgnore]
        public abstract IEnumerable<AFramePreset> BaseFrames { get; }
        
        protected AFrameSequenceComponentData() { }
        protected AFrameSequenceComponentData(string id) : base(id) { }
        
        public override float GetMinTime() => BaseFrames != null && BaseFrames.Any() ? BaseFrames.First().Time : 0f;
        public override float GetMaxTime() => BaseFrames != null && BaseFrames.Any() ? BaseFrames.Last().Time : 0f;
        
        public abstract void StartRecording(float startTime, TTarget target, ReplayRecordingConfig config, CancellationToken cancellationToken = default);
        public abstract void StartPlayback(float startTime, TTarget target, CancellationToken cancellationToken = default);
        public abstract void ApplyTime(TTarget target, float time);
        public abstract void Clear(Predicate<AFramePreset> predicate = null);
    }
    
    
    [Serializable]
    [MemoryPackable(GenerateType.NoGenerate)]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class AFrameSequenceComponentData<TTarget, TPreset> : AFrameSequenceComponentData<TTarget> where TPreset : AFramePreset
    {
        [PropertyOrder(100)]
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("Frames")]
        protected List<TPreset> _Frames = new();

        [MemoryPackIgnore]
        public IEnumerable<TPreset> Frames => _Frames;

        [MemoryPackIgnore]
        public override IEnumerable<AFramePreset> BaseFrames => _Frames;

        protected float _startTimestamp;
        private int _cachedIndex;
        private const int MAX_LINEAR_SCAN_STEPS = 4;

        protected AFrameSequenceComponentData() { }
        protected AFrameSequenceComponentData(string id, List<TPreset> frames) : base(id)
        {
            _Frames = frames;
        }

        public override void StartRecording(float startTime, TTarget target, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
        {
            _startTimestamp = UnityEngine.Time.time;
            Observable
                .Return(Capture(target, startTime))
                .Concat(Observable
                    .Interval(config.Timing.TimeStep, config.Timing.TimeProvider, cancellationToken)
                    .Select(_ => Capture(target, UnityEngine.Time.time - _startTimestamp + startTime))
                    .Chunk(2, 1)
                    .Where(buf => buf.Length == 2 && !AreValuesEqual(buf[0], buf[1]))
                    .SelectMany(buf => buf.ToObservable()))
                .DistinctUntilChangedBy(t => t.Time)
                .Subscribe(val => _Frames.Add(val))
                .RegisterTo(cancellationToken);
        }

        public override void StartPlayback(float startTime, TTarget target, CancellationToken cancellationToken = default)
        {
            _startTimestamp = UnityEngine.Time.time;
            _cachedIndex = 0;
            
            Observable
                .EveryUpdate(cancellationToken)
                .Subscribe(_ =>
                {
                    ApplyTime(target, UnityEngine.Time.time - _startTimestamp + startTime);
                })
                .RegisterTo(cancellationToken);
        }
        
        protected abstract TPreset Capture(TTarget target, float time);
        
        protected abstract void Apply(TTarget target, TPreset beforeFrame, TPreset afterFrame, float blend);

        protected abstract bool AreValuesEqual(TPreset a, TPreset b);
        
        public override void ApplyTime(TTarget target, float time)
        {
            if (TrySample(time, out var beforeFrame, out var afterFrame))
            {
                var delta = afterFrame.Time - beforeFrame.Time;
                var blend = delta > 0f ? Mathf.Clamp01((time - beforeFrame.Time) / delta) : 0f;
                Apply(target, beforeFrame, afterFrame, blend);
            }
        }

        protected bool TrySample(float time, out TPreset beforeFrame, out TPreset afterFrame)
        {
            beforeFrame = null;
            afterFrame = null;

            var frameCount = _Frames.Count;
            if (frameCount == 0)
                return false;

            if (time <= _Frames[0].Time)
            {
                beforeFrame = _Frames[0];
                afterFrame = _Frames[0];
                return true;
            }

            if (time >= _Frames[frameCount - 1].Time)
            {
                beforeFrame = _Frames[frameCount - 1];
                afterFrame = _Frames[frameCount - 1];
                return true;
            }

            var i = _cachedIndex;
            var steps = 0;

            // Try fast linear scan forward
            while (i < frameCount - 1 && _Frames[i + 1].Time < time)
            {
                i++;
                steps++;
                if (steps > MAX_LINEAR_SCAN_STEPS)
                {
                    BinarySearchSample(time, out beforeFrame, out afterFrame);
                    return true;
                }
            }

            // Try fast linear scan backwards
            while (i > 0 && _Frames[i].Time > time)
            {
                i--;
                steps++;
                if (steps > MAX_LINEAR_SCAN_STEPS)
                {
                    BinarySearchSample(time, out beforeFrame, out afterFrame);
                    return true;
                }
            }

            _cachedIndex = i;

            beforeFrame = _Frames[Mathf.Max(0, i)];
            afterFrame = _Frames[Mathf.Min(frameCount - 1, i + 1)];
            return true;
        }

        private void BinarySearchSample(float t, out TPreset firstFrame, out TPreset secondFrame)
        {
            var low = 0;
            var high = _Frames.Count - 1;

            while (low <= high)
            {
                var mid = (low + high) / 2;
                var midTime = _Frames[mid].Time;

                if (Mathf.Approximately(midTime, t))
                {
                    _cachedIndex = mid;
                    firstFrame = _Frames[mid];
                    secondFrame = _Frames[mid];
                    return;
                }

                if (midTime < t)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            var beforeIndex = Mathf.Max(0, high);
            var afterIndex = Mathf.Min(_Frames.Count - 1, low);

            _cachedIndex = beforeIndex;
            firstFrame = _Frames[beforeIndex];
            secondFrame = _Frames[afterIndex];
        }
        
        public override void Clear(Predicate<AFramePreset> predicate = null)
        {
            if (predicate == null)
            {
                _Frames.Clear();
                return;
            }
            _Frames.RemoveAll(predicate);
        }

        protected List<TPreset> GetFramesCopy()
        {
            return _Frames.Select(frame => (TPreset) frame.GetCopy()).ToList();
        }
    }

}
