// Author: František Holubec
// Created: 04.07.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Replay.Frames;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Replay
{
    [Serializable]
    public class ReplayAgentComponent
    {
        [Required]
        [EnhancedChildGameObjectsOnly]
        [OnValueChanged("OnTargetValueChanged", InvokeOnInitialize = true)]
        [EnhancedObjectDrawer]
        [SerializeField]
        private Object _Target;

        [Required]
        [InlineIconButton("Refresh", "RefreshID", "Refresh ID")]
        [SerializeField]
        private string _ID;

        [HideReferenceObjectPicker]
        [EnhancedValidate("ValidateFrameSequences", ContinuousValidationCheck = true)]
        [ValueDropdown("GetFrameSequencesDropdown", IsUniqueList = true, DrawDropdownForListElements = false)]
        [SerializeReference]
        private List<AFrameSequence> _FrameSequences = new();

        public string ID => _ID;
        public Object Target { get => _Target; set => _Target = value; }
        public List<AFrameSequence> FrameSequences => _FrameSequences;

        public float MinTime => _FrameSequences != null && _FrameSequences.Any() ? _FrameSequences.Min(s => s.MinTime) : 0f;
        public float MaxTime => _FrameSequences != null && _FrameSequences.Any() ? _FrameSequences.Max(s => s.MaxTime) : 0f;

        public ReplayAgentComponent() { }
        public ReplayAgentComponent(Object target, string id)
        {
            _Target = target;
            _ID = id;
        }

        public ReplayAgentComponentData GetData()
        {
            return new ReplayAgentComponentData(_ID, _FrameSequences.Select(f => f.GetCopy()).ToList());
        }
        
        public void SetFrames(List<AFrameSequence> frames)
        {
            _FrameSequences.Clear();
            if (frames == null || frames.Count == 0)
                return;
            _FrameSequences.AddRange(frames);
        }

        public void StartRecording(float startTime, ReplayRecordingConfig config, CancellationToken cancellationToken = default)
        {
            if (_FrameSequences == null || _Target == null)
                return;

            foreach (var frameSequence in _FrameSequences)
            {
                frameSequence?.StartCapture(startTime, _Target, config, cancellationToken);
            }
        }

        public void ApplyTime(float time)
        {
            if (_FrameSequences == null || _Target == null)
                return;

            foreach (var frameSequence in _FrameSequences)
            {
                frameSequence?.ApplyTime(_Target, time);
            }
        }
        
        public void ClearFrames(Predicate<AFramePreset> predicate = null)
        {
            if (_FrameSequences == null)
                return;
            
            foreach (var frameSequence in _FrameSequences)
            {
                frameSequence?.Clear(predicate);
            }
        }

        public bool TryGetSequence<TSequence>(out TSequence sequence) where TSequence : AFrameSequence
        {
            return _FrameSequences.TryGetFirstT(out sequence);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnTargetValueChanged(InspectorProperty property)
        {
            if (!string.IsNullOrEmpty(_ID))
                return;
            RefreshID(property);
        }

        private void RefreshID(InspectorProperty property)
        {
            if (Target == null)
                return;

            var root = ((Component) property.SerializationRoot.ValueEntry.WeakSmartValue).gameObject.transform;
            var path = Target.TryGetComponent<Transform>(out var targetTr) ? targetTr.GetPathIn(root) : string.Empty;
            
            _ID = string.IsNullOrEmpty(path) ? Target.GetType().Name : $"{path}-{Target.GetType().Name}";
            property.MarkSerializationRootDirty();
        }

        [UsedImplicitly]
        private IEnumerable GetFrameSequencesDropdown()
        {
            if (Target == null)
                return new List<ValueDropdownItem<AFrameSequence>>();
            
            var targetType = Target.GetType();
            return TypeCacheUtils.GetDerivedClassesOfType<AFrameSequence>()
                .Where(p => p.IsValidFor(targetType))
                .Select(p => new ValueDropdownItem<AFrameSequence>(p.Title, p));
        }

        [UsedImplicitly]
        private void ValidateFrameSequences(List<AFrameSequence> value, SelfValidationResult result, InspectorProperty property)
        {
            if (Target == null)
                return;

            var targetType = Target.GetType();
            foreach (var valuePreset in value)
            {
                if (valuePreset == null)
                {
                    result.AddWarning("Contains null preset!")
                        .WithFix(() => value.RemoveAll(v => v == null));
                    continue;
                }

                if (!valuePreset.IsValidFor(targetType))
                {
                    result.AddError($"Preset '{valuePreset}' is not compatible with Target!")
                        .WithFix(() => value.Remove(valuePreset));
                }
            }
            
            var duplicateItems = value.GroupBy(i => i.GetType())
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateItems.Count > 0)
            {
                result.AddError($"Sequences of duplicate types found")
                    .WithFix(() =>
                    {
                        foreach (var duplicate in duplicateItems)
                        {
                            var first = duplicate.First();
                            value.RemoveAll(v => v.GetType() == first.GetType() && !Equals(v, first));
                        }
                        property.MarkSerializationRootDirty();
                    });
            }
        }
        
        public void InsertSequence(AFrameSequence sequence, int index = 0)
        {
            _FrameSequences.Insert(index, sequence);
        }
#endif
    }
}
