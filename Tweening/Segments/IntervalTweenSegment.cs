using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public class IntervalTweenSegment : IDirectTweenSegment, IPresetTweenSegment
    {
        [SerializeField]
        [HideLabel]
        private AdditionOperation _Operation;
        
        [SerializeField]
        [HideLabel]
        [SuffixLabel("s", true)]
        private float _Interval;

        public void AddToSequence(Sequence sequence, Object target) => AddToSequence(sequence);
        
        public void AddToSequence(Sequence sequence)
        {
            switch (_Operation)
            {
                case AdditionOperation.AppendInterval:
                    sequence.AppendInterval(_Interval);
                    break;
                case AdditionOperation.PrependInterval: 
                    sequence.PrependInterval(_Interval);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public IntervalTweenSegment() { }

        public enum AdditionOperation
        {
            AppendInterval,
            PrependInterval
        }
        
#if UNITY_EDITOR
        public void PopulateReferences(HashSet<TweenObjectReference> references) { }
        public void PopulateTargets(TweenTargetCollection targets) { }

        public bool TryConvertToDirectSegment(out IDirectTweenSegment result, IDictionary<TweenObjectReference, Object> targets)
        {
            result = this.GetCopy();
            return true;
        }

        public bool TryConvertToPresetSegment(out IPresetTweenSegment result, IDictionary<Object, TweenObjectReference> references)
        {
            result = this.GetCopy();
            return true;
        }

        public IntervalTweenSegment(AdditionOperation operation, float interval, Type targetType)
        {
            _Operation = operation;
            _Interval = interval;
            TargetType = targetType;
        }

        public string GetSummary()
        {
            return $"{_Operation.ToString().Nicify()} {_Interval}s";
        }

        public string LabelName => "Interval";

        public Type TargetType { get; set; }
#endif
    }
}
