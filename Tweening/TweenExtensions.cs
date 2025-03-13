using DG.Tweening;

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
    }
}
