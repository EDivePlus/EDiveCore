using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Tweening.ObjectActions;
using EDIVE.Utils.ObjectActions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public abstract class AObjectTweenSegment : ABaseTweenSegment
    {
        [PropertyOrder(10)]
        [SerializeReference]
        [LabelWidth(120)]
        [LabelText("Action")]
        [EnhancedValueDropdown("TweenActionsDropdown", ChildrenDisplayType = DropdownChildrenDisplayType.ShowChildrenInline, SortDropdownItems = true)]
        [ValidateInput("ValidateObjectAction", ContinuousValidationCheck = true)]
        protected ATweenObjectAction _ObjectAction;

        public override void AddToSequence(Sequence sequence)
        {
            if (!TryGetTarget(out var target))
            {
                OnInvalidTarget(sequence);
                return;
            }

            if (_ObjectAction == null || !_ObjectAction.TryGetTween(target, out var tween))
            {
                OnInvalidObjectAction(sequence);
                return;
            }

            AddToSequence(sequence, tween);
        }

        protected abstract bool TryGetTarget(out Object target);

        protected virtual void OnInvalidTarget(Sequence sequence) { }
        protected virtual void OnInvalidObjectAction(Sequence sequence) { }

        protected AObjectTweenSegment() { }
        protected AObjectTweenSegment(ATweenObjectAction objectAction, TweenAdditionType operation, float insertionPosition = 0f) : base(operation, insertionPosition)
        {
            _ObjectAction = objectAction;
        }

#if UNITY_EDITOR
        public override string GetSummary()
        {
            return $"{GetSummaryPrefix()} '{(TryGetTargetName(out var targetName) ? targetName : "Unknown Target")}' Do {_ObjectAction?.ToString() ?? "Undefined Action"}";
        }

        protected abstract bool TryGetTargetName(out string targetName);
        protected abstract bool TryGetTargetType(out Type targetType);

        [UsedImplicitly]
        private bool ValidateObjectAction(ATweenObjectAction objectAction, ref string errorMessage, ref InfoMessageType? messageType)
        {
            if (!TryGetTargetType(out var targetType))
                return true;

            if (objectAction == null)
            {
                errorMessage = "Object action not assigned!";
                messageType = InfoMessageType.Warning;
                return false;
            }

            if (!objectAction.IsValidFor(targetType))
            {
                errorMessage = $"Object action is not valid for target of type '{targetType.Name}'!";
                messageType = InfoMessageType.Error;
                return false;
            }

            return true;
        }

        [UsedImplicitly]
        private IEnumerable<ValueDropdownItem<ATweenObjectAction>> TweenActionsDropdown()
        {
            return TryGetTargetType(out var targetType) 
                ? ObjectActionUtils.GetTweenActionsDropdown<ATweenObjectAction>(targetType) 
                : null;
        }
#endif
    }
}
