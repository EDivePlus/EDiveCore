using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public abstract class ACallbackTweenSegment : ITweenSegment
    {
        [SerializeField]
        [HideLabel]
        protected AdditionOperation _Operation;

        [SerializeField]
        [SuffixLabel("s", true)]
        [ShowIf(nameof(_Operation), AdditionOperation.Insert)]
        protected float _Position;

        public void AddToSequence(Sequence sequence, UnityEngine.Object target) => AddToSequence(sequence);

        public void AddToSequence(Sequence sequence)
        {
            switch (_Operation)
            {
                case AdditionOperation.Append:
                    sequence.AppendCallback(GetCallbackAction());
                    break;
                case AdditionOperation.Prepend:
                    sequence.PrependCallback(GetCallbackAction());
                    break;
                case AdditionOperation.Insert:
                    sequence.InsertCallback(_Position, GetCallbackAction());
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        protected abstract TweenCallback GetCallbackAction();

        public enum AdditionOperation
        {
            Append,
            Prepend,
            Insert
        }

#if UNITY_EDITOR
        public abstract string LabelName { get; }
        public Type TargetType { get; set; }

        public abstract string GetSummary();
        public virtual void PopulateReferences(HashSet<TweenObjectReference> references) { }
        public virtual void PopulateTargets(TweenTargetCollection targets) { }
#endif

    }
}
