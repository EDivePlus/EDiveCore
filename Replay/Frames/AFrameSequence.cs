// Author: František Holubec
// Created: 04.07.2025

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MemoryPack;
using Newtonsoft.Json;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Replay.Frames
{
    [Serializable]
    [MemoryPackable(GenerateType.NoGenerate)]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class AFrameSequence
    {
        [MemoryPackIgnore]
        public abstract string Title { get; }

        [MemoryPackIgnore]
        public abstract Type TargetType { get; }

        [MemoryPackIgnore]
        public abstract IEnumerable<AFramePreset> BaseFrames { get; }

        [MemoryPackIgnore]
        public float MinTime => BaseFrames != null && BaseFrames.Any() ? BaseFrames.First().Time : 0f;

        [MemoryPackIgnore]
        public float MaxTime => BaseFrames != null && BaseFrames.Any() ? BaseFrames.Last().Time : 0f;

        public bool IsValidFor(Type targetType) => TargetType.IsAssignableFrom(targetType);

        public abstract void StartCapture(float startTime, object target, ReplayRecordingConfig config, CancellationToken cancellationToken = default);
        public abstract void ApplyTime(object targetObject, float time);
        public abstract void Clear(Predicate<AFramePreset> predicate = null);

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType();
        }

        protected bool Equals(AFrameSequence other) => other.GetType() == GetType();
        public override int GetHashCode() => GetType().GetHashCode();
        public abstract AFrameSequence GetCopy();
        
#if UNITY_EDITOR
        [PropertyOrder(-100)]
        [OnInspectorGUI]
        public void DrawTitle() => GUILayout.Label(Title, SirenixGUIStyles.BoldLabel);
#endif
    }

    [Serializable]
    [MemoryPackable(GenerateType.NoGenerate)]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class AFrameSequence<TTarget, TPreset> : AFrameSequence where TPreset : AFramePreset
    {
        [PropertyOrder(100)]
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("Frames")]
        protected List<TPreset> _Frames = new();

        [MemoryPackIgnore]
        public override Type TargetType => typeof(TTarget);

        [MemoryPackIgnore]
        public IEnumerable<TPreset> Frames => _Frames;

        [MemoryPackIgnore]
        public override IEnumerable<AFramePreset> BaseFrames => _Frames;

        protected float _startCaptureTime;
        private int _cachedIndex;
        private const int MAX_LINEAR_SCAN_STEPS = 4;

        protected AFrameSequence() { }
        protected AFrameSequence(List<TPreset> frames) => _Frames = frames;

        public override void StartCapture(float startTime, object target, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
        {
            if (target is TTarget tTarget) StartCapture(startTime, tTarget, config, cancellationToken);
        }

        protected virtual void StartCapture(float startTime, TTarget target, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
        {
            _startCaptureTime = UnityEngine.Time.time;
            Observable
                .Return(Capture(target, startTime))
                .Concat(Observable
                    .Interval(config.Timing.TimeStep, config.Timing.TimeProvider, cancellationToken)
                    .Select(_ => Capture(target, UnityEngine.Time.time - _startCaptureTime + startTime))
                    .Chunk(2, 1)
                    .Where(buf => buf.Length == 2 && !AreValuesEqual(buf[0], buf[1]))
                    .SelectMany(buf => buf.ToObservable()))
                .DistinctUntilChangedBy(t => t.Time)
                .Subscribe(val => _Frames.Add(val))
                .RegisterTo(cancellationToken);
        }

        protected abstract bool AreValuesEqual(TPreset a, TPreset b);

        protected abstract TPreset Capture(TTarget target, float time);

        public override void ApplyTime(object targetObject, float time)
        {
            if (targetObject is TTarget tTarget && TrySample(time, out var beforeFrame, out var afterFrame))
            {
                var delta = afterFrame.Time - beforeFrame.Time;
                var blend = delta > 0f ? Mathf.Clamp01((time - beforeFrame.Time) / delta) : 0f;
                Apply(tTarget, beforeFrame, afterFrame, blend);
            }
        }

        protected abstract void Apply(TTarget target, TPreset beforeFrame, TPreset afterFrame, float blend);

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
