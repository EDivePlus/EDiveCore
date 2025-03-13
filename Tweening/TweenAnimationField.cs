using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening
{
    [Serializable]
    public class TweenAnimationField : ATweenAnimationField
    {
        [PropertySpace]
        [PropertyOrder(10)]
        [SerializeField]
        private TweenObjectReferenceCollection _References;
        
        [SerializeField]
        protected TweenSequence _Sequence;

        protected override void PopulateSequence(Sequence sequence)
        {
            _References.AssignTempReferences();
            _Sequence?.PopulateSequence(sequence);
            _References.ClearTempReferences();
        }

        public override IDictionary<TweenObjectReference, Object> GetReferencesDictionary()
        {
            return _References?.GetReferencesDictionary();
        }

#if UNITY_EDITOR
        public override void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            _Sequence?.PopulateReferences(references);
        }

        public override void PopulateTargets(TweenTargetCollection targets)
        {
            _Sequence?.PopulateTargets(targets);
        }
#endif
    }
}
