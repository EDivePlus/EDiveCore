using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.Selectables
{
    public class TweenSelectableTransitionPreset : ATweenPreset
    {
        [PropertySpace]
        [SerializeField]
        [OnValueChanged("RefreshReferences", true)]
        private TweenSequence _NormalAnimation;

        [SerializeField]
        [OnValueChanged("RefreshReferences", true)]
        private TweenSequence _HighlightedAnimation;

        [SerializeField]
        [OnValueChanged("RefreshReferences", true)]
        private TweenSequence _PressedAnimation;

        [SerializeField]
        [OnValueChanged("RefreshReferences", true)]
        private TweenSequence _SelectedAnimation;

        [SerializeField]
        [OnValueChanged("RefreshReferences", true)]
        private TweenSequence _DisabledAnimation;

        private TweenSequence GetTweenSequenceByState(SelectionState state) => state switch
        {
            SelectionState.Normal => _NormalAnimation,
            SelectionState.Highlighted => _HighlightedAnimation,
            SelectionState.Pressed => _PressedAnimation,
            SelectionState.Selected => _SelectedAnimation,
            SelectionState.Disabled => _DisabledAnimation,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        public Sequence CreateSequence(SelectionState state, TweenObjectReferenceCollection references, bool instant = false)
        {
            references.AssignTempReferences();
            var anim = GetTweenSequenceByState(state);
            var sequence = anim.CreateSequence();
            sequence.SetUpdate(true);
            references.ClearTempReferences();
            return sequence;
        }

#if UNITY_EDITOR
        public override void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            _NormalAnimation.PopulateReferences(references);
            _HighlightedAnimation.PopulateReferences(references);
            _PressedAnimation.PopulateReferences(references);
            _SelectedAnimation.PopulateReferences(references);
            _DisabledAnimation.PopulateReferences(references);
        }

        public override void PopulateTargets(TweenTargetCollection targets)
        {
            _NormalAnimation.PopulateTargets(targets);
            _HighlightedAnimation.PopulateTargets(targets);
            _PressedAnimation.PopulateTargets(targets);
            _SelectedAnimation.PopulateTargets(targets);
            _DisabledAnimation.PopulateTargets(targets);
        }
#endif
    }
}
