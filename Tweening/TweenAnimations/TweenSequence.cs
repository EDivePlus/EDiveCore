using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.Tweening.Segments;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Tweening
{
    [Serializable]
    [InlineProperty]
    [HideLabel]
    public class TweenSequence : ITweenTargetProvider
    {
        [FormerlySerializedAs("_Sequence")]
        [InlineProperty]
        [SerializeReference]
        [HideReferenceObjectPicker]
        [LabelText("@$property.Parent.NiceName")]
        [ListDrawerSettings(OnTitleBarGUI = "OnSequenceTitleBarGUI")]
        [ValueDropdown("GetAllSequenceParts", DrawDropdownForListElements = false)]
        private List<ITweenSegment> _Segments = new();
        
        public IReadOnlyList<ITweenSegment> Segments => _Segments;

        public Sequence CreateSequence()
        {
            var sequence = DOTween.Sequence();
            PopulateSequence(sequence);
            return sequence;
        }
        
        public void PopulateSequence(Sequence sequence)
        {
            foreach (var animPart in _Segments)
            {
                animPart?.AddToSequence(sequence);
            }
        }

        public TweenSequence() { }
        public TweenSequence(List<ITweenSegment> segments)
        {
            _Segments = segments;
        }

#if UNITY_EDITOR
        public void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            foreach (var segment in _Segments)
            {
                segment?.PopulateReferences(references);
            }
        }

        public void PopulateTargets(TweenTargetCollection targets)
        {
            foreach (var segment in _Segments)
            {
                segment?.PopulateTargets(targets);
            }
        }

        [UsedImplicitly]
        protected void OnSequenceTitleBarGUI(InspectorProperty property)
        {
            TweenEditorUtils.DrawSavePresetToolbarButton(_Segments, property);
        }

        [UsedImplicitly]
        protected IEnumerable<ValueDropdownItem<ITweenSegment>> GetAllSequenceParts(InspectorProperty property)
        {
            return TweenEditorUtils.GetAllSequenceSegments(property);
        }
        
        public bool TryConvertToDirectSequence(out TweenSequence result, IDictionary<TweenObjectReference, Object> targets)
        {
            result = new TweenSequence(_Segments.ConvertToDirectSegments(targets));
            return true;
        }

        public bool TryConvertToPresetSequence(out TweenSequence result, IDictionary<Object, TweenObjectReference> references)
        {
            result = new TweenSequence(_Segments.ConvertToPresetSegments(references));
            return true;
        }
#endif
    }
}
