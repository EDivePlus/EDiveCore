using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Tweening
{
    public static class TweenExtensions
    {
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
