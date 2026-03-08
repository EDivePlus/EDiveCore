using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tweening
{
    [Serializable]
    public class TweenAdditionOperation
    {
        [SerializeField]
        [HideLabel]
        [HorizontalGroup("Operation")]
        private TweenAdditionType _Operation;

        [ShowIf(nameof(_Operation), TweenAdditionType.Insert)]
        [Indent]
        [LabelWidth(40)]
        [SerializeField]
        [LabelText("At")]
        [SuffixLabel("s", true)]
        [HorizontalGroup("Operation", 110)]
        private float _InsertionPosition;

        public Sequence Apply(Sequence sequence, Tween tween)
        {
            return sequence.AddToSequence(tween, _Operation, _InsertionPosition);
        }

        public string GetSummaryPrefix()
        {
            return _Operation.GetSummaryPrefix(_InsertionPosition);
        }
    }

    public static class TweenAdditionOperationExtensions
    {
        public static Sequence ApplyOperation(this Sequence sequence, Tween tween, TweenAdditionOperation operation)
        {
            return operation.Apply(sequence, tween);
        }
    }
}
