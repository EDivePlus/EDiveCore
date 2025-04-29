// Author: František Holubec
// Created: 26.04.2024

using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.UIElements.Selectables
{
    [Serializable]
    public class TweenSelectableTransition : ASelectableTransition, ITweenTargetProvider, ITweenReferencesHolder
    {
        [ShowCreateNew]
        [SerializeField]
        private TweenSelectableTransitionPreset _Preset;

        [SerializeField]
        private TweenObjectReferenceCollection _References;

        private Sequence _tweener;

        public override void DoStateTransition(SelectionState state, bool instant = false)
        {
            if (_Preset == null) return;

            _tweener?.Kill();
#if UNITY_EDITOR
            if (!Application.isPlaying) DOTweenEditorPreview.RemoveTweenFromPreview(_tweener);
#endif
            _tweener = _Preset.CreateSequence(state, _References, instant);
            _tweener.Play();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DOTweenEditorPreview.PrepareTweenForPreview(_tweener);
                DOTweenEditorPreview.Start();
            }
#endif
            if (instant)
            {
                _tweener?.Complete(true);
                _tweener?.Kill();
#if UNITY_EDITOR
                if (!Application.isPlaying) DOTweenEditorPreview.RemoveTweenFromPreview(_tweener);
#endif
                _tweener = null;
            }
        }

        public IDictionary<TweenObjectReference, Object> GetReferencesDictionary()
        {
            return _References?.GetReferencesDictionary();
        }

#if UNITY_EDITOR
        public void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            if (_Preset != null)
            {
                _Preset.PopulateReferences(references);
            }
        }

        public void PopulateTargets(TweenTargetCollection targets)
        {
            if (_Preset != null)
            {
                _Preset.PopulateTargets(targets);
            }
        }
#endif
    }
}
