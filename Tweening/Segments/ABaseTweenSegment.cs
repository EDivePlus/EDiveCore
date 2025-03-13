using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public abstract class ABaseTweenSegment : ITweenSegment
    {
        [SerializeField]
        [HideLabel]
        [HorizontalGroup("Operation")]
        protected TweenAdditionType _Operation;

        [ShowIf(nameof(_Operation), TweenAdditionType.Insert)]
        [Indent]
        [LabelWidth(40)]
        [SerializeField]
        [LabelText("At")]
        [SuffixLabel("s", true)]
        [HorizontalGroup("Operation", 110)]
        protected float _InsertionPosition;

        protected void AddToSequence(Sequence sequence, Tween tween)
        {
            sequence.AddToSequence(tween, _Operation, _InsertionPosition);
        }

        public string GetSummaryPrefix()
        {
            return _Operation.GetSummaryPrefix(_InsertionPosition);
        }

        public abstract void AddToSequence(Sequence sequence);

        protected ABaseTweenSegment() { }
        protected ABaseTweenSegment(TweenAdditionType operation, float insertionPosition = 0f)
        {
            _Operation = operation;
            _InsertionPosition = insertionPosition;
        }

#if UNITY_EDITOR
        public abstract string GetSummary();
        public abstract string LabelName { get; }
        public virtual void PopulateReferences(HashSet<TweenObjectReference> references) { }
        public virtual void PopulateTargets(TweenTargetCollection targets) { }
#endif
    }
}
