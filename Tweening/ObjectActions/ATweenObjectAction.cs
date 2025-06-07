using System;
using DG.Tweening;
using EDIVE.NativeUtils;
using EDIVE.Utils.ObjectActions;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public abstract class ATweenObjectAction : IObjectAction
    {
        [SerializeField]
        [MinValue(0)]
        protected float _Delay;

        [SerializeField]
        [MinValue(0)]
        protected float _Duration = 1;

        [SerializeField]
        [HideIf(nameof(IsDurationZero))]
        protected EaseBundle _Ease;

        private bool IsDurationZero => Mathf.Approximately(_Duration, 0);

        public float Delay => _Delay;
        public float Duration => _Duration;
        public EaseBundle Ease => _Ease;

        public abstract Type TargetType { get; }

        public abstract bool TryGetTween(Object target, out Tween tween);
        public abstract bool IsValidFor(Type targetType);

        public ATweenObjectAction GetCopy()
        {
            return JsonUtility.FromJson(JsonUtility.ToJson(this), GetType()) as ATweenObjectAction;
        }

        public override string ToString() => GetType().Name.Replace("TweenAction", "").Nicify();
    }
    
    [Serializable]
    public abstract class ATweenObjectAction<T> : ATweenObjectAction where T : Object
    {
        public override Type TargetType => typeof(T);

        public override bool TryGetTween(Object target, out Tween tween)
        {
            tween = null;
            if (target is not T tTarget)
                return false;

            tween = GetTween(tTarget);
            if (tween == null)
                return false;

            if (!Mathf.Approximately(_Duration, 0))
            {
                tween.SetEase(_Ease);
            }
            tween.SetDelay(_Delay);
            return true;
        }
        
        public override bool IsValidFor(Type targetType) => typeof(T).IsAssignableFrom(targetType);
        
        protected abstract Tween GetTween(T target);
    }
}
