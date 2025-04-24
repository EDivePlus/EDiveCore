using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Tweening.ObjectActions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public class ObjectTweenSegment : AObjectTweenSegment, IDirectTweenSegment
    {
        [SerializeField]
        [LabelWidth(120)]
        [DontValidate]
        [EnhancedObjectDrawer("IsValidTarget", PreferredTypeGetter = "GetPreferredTargetType")]
        private Object _Target;

        protected override bool TryGetTarget(out Object target)
        {
            target = _Target;
            return target != null;
        }

        public ObjectTweenSegment() { }

        public ObjectTweenSegment(Object target, ATweenObjectAction objectAction, TweenAdditionType operation, float insertionPosition = 0f) : base(objectAction, operation, insertionPosition)
        {
            _Target = target;
        }

#if UNITY_EDITOR
        public bool TryConvertToPresetSegment(out IPresetTweenSegment result, IDictionary<Object, TweenObjectReference> references)
        {
            result = new ObjectReferenceTweenSegment(references.TryGetValue(_Target, out var reference) ? reference : null, _ObjectAction.GetCopy(), _Operation, _InsertionPosition);
            return true;
        }

        public override void PopulateTargets(TweenTargetCollection targets)
        {
            base.PopulateTargets(targets);
            targets.Add(_Target, _ObjectAction.TargetType);
        }

        public override string LabelName => "Tween";

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
            targetType = _Target.GetType();
            return true;
        }

        [UsedImplicitly]
        private Type GetPreferredTargetType() => _ObjectAction?.TargetType;

        [UsedImplicitly]
        private bool IsValidTarget(Object target) => TypeCacheUtils.GetDerivedClassesOfType<ATweenObjectAction>().Any(tweenAction => tweenAction.IsValidFor(target.GetType()));
#endif
    }
}
