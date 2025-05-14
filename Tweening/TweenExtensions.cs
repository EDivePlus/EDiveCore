using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Tweening
{
    public static class TweenExtensions
    {
        public static IEnumerable<T> GetAllTargets<T>(this Tween tween)
        {
            foreach (var target in GetAllTargets(tween))
            {
                if (target is T tTarget && (tTarget is not Object gameObject || gameObject))
                    yield return tTarget;
            }
        }

        public static IEnumerable<T> GetAllTargets<T>(this IEnumerable<Tween> tweens)
        {
            foreach (var target in GetAllTargets(tweens))
            {
                if (target is T tTarget && (tTarget is not Object gameObject || gameObject))
                    yield return tTarget;
            }
        }

        public static IEnumerable<object> GetAllTargets(this Tween tween)
        {
            if (tween is Tweener tweener)
                yield return tweener.target;
            else if (tween is Sequence sequence)
                foreach (var target in GetAllTargets(sequence.GetTweens()))
                    yield return target;
        }

        public static IEnumerable<object> GetAllTargets(this IEnumerable<Tween> tweens)
        {
            foreach (var tween in tweens)
            foreach (var target in GetAllTargets(tween))
                yield return target;
        }

        private static FieldInfo _sequenceTweensField;
        public static List<Tween> GetTweens(this Sequence sequence)
        {
            if (sequence == null) return null;
            _sequenceTweensField ??= typeof(Sequence).GetField("sequencedTweens", BindingFlags.NonPublic | BindingFlags.Instance);
            return _sequenceTweensField!.GetValue(sequence) as List<Tween>;
        }

        public static T SetEase<T>(this T tween, EaseBundle easeBundle) where T : Tween
        {
            return easeBundle.SetEase(tween);
        }
        
        public static T OnEnded<T>(this T tween, TweenCallback action, bool preventRetrigger = true) where T : Tween
        {
            var alreadyTriggered = false;
            
            return tween
                .OnKill(PreventRetrigger)
                .OnComplete(PreventRetrigger);
            
            void PreventRetrigger()
            {
                if (preventRetrigger && alreadyTriggered)
                    return;

                action.Invoke();
                
                alreadyTriggered = true;
            }
        }

        public static TweenerCore<Vector3, Vector3, VectorOptions> DOMoveXY(
            this Transform target,
            Vector3 endValue,
            float duration)
        {
            return DOTween.To(() => target.position, x => target.position = target.position.WithXY(x.x, x.y), endValue, duration);
        }

        public static TweenerCore<Vector3, Vector3, VectorOptions> DOMoveXZ(
            this Transform target,
            Vector3 endValue,
            float duration)
        {
            return DOTween.To(() => target.position, x => target.position = target.position.WithXZ(x.x, x.z), endValue, duration);
        }

        public static TweenerCore<Vector3, Vector3, VectorOptions> DOMoveYZ(
            this Transform target,
            Vector3 endValue,
            float duration)
        {
            return DOTween.To(() => target.position, x => target.position = target.position.WithYZ(x.y, x.z), endValue, duration);
        }
    }
}
