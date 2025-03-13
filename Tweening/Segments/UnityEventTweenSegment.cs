using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.NativeUtils;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public class UnityEventTweenSegment : ACallbackTweenSegment, IDirectTweenSegment
    {
        [SerializeField]
        public UnityEvent _Event = new();

        public UnityEventTweenSegment() { }
        
        public UnityEventTweenSegment(UnityEvent unityEvent)
        {
            _Event = unityEvent;
        }

        protected override TweenCallback GetCallbackAction() => CallbackAction;

        private void CallbackAction()
        {
            _Event?.Invoke();
        }

#if UNITY_EDITOR
        public UnityEventTweenSegment(AdditionOperation operation, float position, Type targetType)
        {
            _Operation = operation;
            _Position = position;
            TargetType = targetType;
        }

        public override string GetSummary()
        {
            return $"{_Operation.ToString().Nicify()} Unity Event [{_Event.GetSummary()}]";
        }

        public override string LabelName => "Unity Event";

        public bool TryConvertToPresetSegment(out IPresetTweenSegment result, IDictionary<Object, TweenObjectReference> references)
        {
            result = null;
            return false;
        }
#endif
    }
}
