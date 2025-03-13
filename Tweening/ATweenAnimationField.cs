using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening
{
    [Serializable]
    public abstract class ATweenAnimationField : ATweenAnimationPlayer, ITweenTargetProvider, ITweenReferencesHolder
    {
        [EnhancedBoxGroup("BaseSetup")]
        [SerializeField]
        private float _TimeScale = 1;

        [EnhancedBoxGroup("BaseSetup")]
        [Tooltip("If true, this Sequence will ignore the current Time Scale of the application (Time.timeScale).")]
        [SerializeField]
        private bool _IgnoreUnityTimeScale;

        [EnhancedBoxGroup("BaseSetup")]
        [Tooltip("DOTween's update type for THIS Sequence.\n\nNormal: updates on Update() calls.\nFixed: updates on FixedUpdate() calls.\nLate: updates on LateUpdate() calls.")]
        [SerializeField]
        private UpdateType _UpdateTime = UpdateType.Normal;

        public override Sequence CreateSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.SetUpdate(_UpdateTime, _IgnoreUnityTimeScale);
            sequence.timeScale = _TimeScale;
            PopulateSequence(sequence);
            return sequence;
        }

        protected abstract void PopulateSequence(Sequence sequence);
        public abstract IDictionary<TweenObjectReference, Object> GetReferencesDictionary();

#if UNITY_EDITOR
        public abstract void PopulateReferences(HashSet<TweenObjectReference> references);
        public abstract void PopulateTargets(TweenTargetCollection targets);

        [PropertySpace(SpaceBefore = 0, SpaceAfter = 4)]
        [PropertyOrder(-100)]
        [OnInspectorGUI]
        protected virtual void DrawControls()
        {
            TweenEditorUtils.DrawControls(this);
        }
#endif
    }
}

