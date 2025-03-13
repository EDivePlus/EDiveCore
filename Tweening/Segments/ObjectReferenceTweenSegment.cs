using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Tweening.ObjectActions;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public class ObjectReferenceTweenSegment : AObjectTweenSegment, IPresetTweenSegment
    {
        [Required]
        [SerializeField]
        [LabelWidth(120)]
        [ShowCreateNew]
        private TweenObjectReference _Target;

        [Tooltip("What to do when the target is invalid.")]
        [LabelWidth(120)]
        [SerializeField]
        private InvalidTargetAction _InvalidAction;

        protected override bool TryGetTarget(out Object target)
        {
            target = null;
            return _Target != null && _Target.TryGetTempValue(out target);
        }

        protected override void OnInvalidTarget(Sequence sequence)
        {
            switch (_InvalidAction)
            {
                case InvalidTargetAction.Ignore:
                    break;
                case InvalidTargetAction.ReplaceWithInterval:
                    TryReplaceWithInterval(sequence);
                    break;
                case InvalidTargetAction.LogError:
                    Debug.LogError($"Invalid target '{(_Target != null ? _Target.name : "Unknown")}' in tween segment!");
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private bool TryReplaceWithInterval(Sequence sequence)
        {
            if (_ObjectAction == null)
                return false;

            var duration = _ObjectAction.Delay + _ObjectAction.Duration;
            switch (_Operation)
            {
                case TweenAdditionType.Append:
                    sequence.AppendInterval(duration);
                    return true;

                case TweenAdditionType.Prepend:
                    sequence.PrependInterval(duration);
                    return true;

                case TweenAdditionType.Insert:
                case TweenAdditionType.Join:
                default:
                    return false;
            }
        }

        public ObjectReferenceTweenSegment() { }
        public ObjectReferenceTweenSegment( TweenObjectReference target, ATweenObjectAction objectAction) : base(objectAction)
        {
            _Target = target;
        }

        private enum InvalidTargetAction
        {
            Ignore,
            ReplaceWithInterval,
            LogError
        }

#if UNITY_EDITOR
        public override void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            references.Add(_Target);
        }

        public override string LabelName => "Tween";

        public bool TryConvertToDirectSegment(out IDirectTweenSegment result, IDictionary<TweenObjectReference, Object> targets)
        {
            result = new ObjectTweenSegment(targets.TryGetValue(_Target, out var target) ? target : null, _ObjectAction.GetCopy());
            return true;
        }

        protected override bool TryGetTargetName(out string targetName)
        {
            targetName = null;
            if (_Target == null) return false;
            targetName = _Target.name;
            return true;
        }

        protected override bool TryGetTargetType(out Type targetType)
        {
            targetType = null;
            if (_Target == null) return false;
            targetType = _Target.ValueType;
            return true;
        }
#endif
    }
}

