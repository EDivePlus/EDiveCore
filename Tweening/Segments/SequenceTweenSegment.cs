using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public class SequenceTweenSegment : ABaseTweenSegment, IDirectTweenSegment, IPresetTweenSegment, IParentTweenSegment
    {
        [PropertyOrder(20)]
        [SerializeField]
        private TweenSequence _Sequence = new ();
        
        public override void AddToSequence(Sequence sequence)
        {
            var subSequence = DOTween.Sequence();
            _Sequence?.PopulateSequence(subSequence);
            AddToSequence(sequence, subSequence);
        }

        public SequenceTweenSegment() { }
        public SequenceTweenSegment(TweenSequence sequence)
        {
            _Sequence = sequence;
        }

        public SequenceTweenSegment(TweenSequence sequence, TweenAdditionType operation, float insertionPosition = 0) : base(operation, insertionPosition)
        {
            _Sequence = sequence;
        }

        public IEnumerable<ITweenSegment> GetChildSegments()
        {
            return _Sequence?.Segments ?? Enumerable.Empty<ITweenSegment>();
        }

#if UNITY_EDITOR
        public bool TryConvertToDirectSegment(out IDirectTweenSegment result, IDictionary<TweenObjectReference, Object> targets)
        {
            if (_Sequence.TryConvertToDirectSequence(out var convertedSequence, targets))
            {
                result = new SequenceTweenSegment(convertedSequence);
                return true;
            }
            result = null;
            return false;
        }

        public bool TryConvertToPresetSegment(out IPresetTweenSegment result, IDictionary<Object, TweenObjectReference> references)
        {
            if (_Sequence.TryConvertToPresetSequence(out var convertedSequence, references))
            {
                result = new SequenceTweenSegment(convertedSequence);
                return true;
            }
            result = null;
            return false;
        }

        public override string LabelName => "Sequence";
        
        public override string GetSummary() => $"{GetSummaryPrefix()} Sequence";

        public override void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            base.PopulateReferences(references);
            _Sequence?.PopulateReferences(references);
        }

        public override void PopulateTargets(TweenTargetCollection targets)
        {
            base.PopulateTargets(targets);
            _Sequence?.PopulateTargets(targets);
        }
#endif
    }
}
