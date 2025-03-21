using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening
{
    [Serializable]
    public abstract class AParentedTweenAnimationField<TParent> : ATweenAnimationField where TParent : MonoBehaviour
    {
        [PropertySpace(4)]
        [EnhancedBoxGroup("BaseSetup")]
        [SerializeField]
        private TweenObjectReferenceCollection _References;

        [EnhancedBoxGroup("BaseSetup")]
        [SerializeField]
        private List<AdditiveTweenSequence> _AdditiveSequences = new();

        protected TParent Parent { get; private set; }

        public void Initialize(TParent controller)
        {
            Parent = controller;
        }

        protected sealed override void PopulateSequence(Sequence sequence)
        {
            PopulateSequenceDynamic(sequence);
            _References.AssignTempReferences();
            foreach (var additiveSequence in _AdditiveSequences)
            {
                additiveSequence.ApplyTo(sequence);
            }
            _References.ClearTempReferences();
        }

        protected abstract void PopulateSequenceDynamic(Sequence sequence);

        public override IDictionary<TweenObjectReference, Object> GetReferencesDictionary()
        {
            return _References?.GetReferencesDictionary();
        }

#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            Parent = property.GetParentObject<TParent>();
        }

        public override void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            foreach (var sequence in _AdditiveSequences) sequence?.PopulateReferences(references);
        }

        public override void PopulateTargets(TweenTargetCollection targets)
        {
            foreach (var sequence in _AdditiveSequences) sequence?.PopulateTargets(targets);
        }

        [EnhancedBoxGroup("BaseSetup", "@ColorTools.Yellow", false, SpaceAfter = 4)]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 4)]
        [PropertyOrder(-100)]
        [OnInspectorGUI]
        protected override void DrawControls()
        {
            TweenEditorUtils.DrawControls(this);
        }
#endif
    }
}
