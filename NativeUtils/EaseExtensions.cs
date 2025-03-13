using DG.Tweening;

namespace EDIVE.NativeUtils
{
    public static class EaseExtensions
    {
        public static float Evaluate(this Ease ease, float time) => ease.Evaluate(0, 1, time);

        public static float Evaluate(this Ease ease, float from, float to, float time) => DOVirtual.EasedValue(from, to, time, ease);

        public static Ease GetInverse(this Ease ease) => ease switch
        {
            Ease.Linear => Ease.Linear,
            Ease.InSine => Ease.OutSine,
            Ease.OutSine => Ease.InSine,
            Ease.InOutSine => Ease.InOutSine,
            Ease.InQuad => Ease.OutQuad,
            Ease.OutQuad => Ease.InQuad,
            Ease.InOutQuad => Ease.InOutQuad,
            Ease.InCubic => Ease.OutCubic,
            Ease.OutCubic => Ease.InCubic,
            Ease.InOutCubic => Ease.InOutCubic,
            Ease.InQuart => Ease.OutQuart,
            Ease.OutQuart => Ease.InQuart,
            Ease.InOutQuart => Ease.InOutQuart,
            Ease.InQuint => Ease.OutQuint,
            Ease.OutQuint => Ease.InQuint,
            Ease.InOutQuint => Ease.InOutQuint,
            Ease.InExpo => Ease.OutExpo,
            Ease.OutExpo => Ease.InExpo,
            Ease.InOutExpo => Ease.InOutExpo,
            Ease.InCirc => Ease.OutCirc,
            Ease.OutCirc => Ease.InCirc,
            Ease.InOutCirc => Ease.InOutCirc,
            Ease.InElastic => Ease.OutElastic,
            Ease.OutElastic => Ease.InElastic,
            Ease.InOutElastic => Ease.InOutElastic,
            Ease.InBack => Ease.OutBack,
            Ease.OutBack => Ease.InBack,
            Ease.InOutBack => Ease.InOutBack,
            Ease.InBounce => Ease.OutBounce,
            Ease.OutBounce => Ease.InBounce,
            Ease.InOutBounce => Ease.InOutBounce,
            _ => Ease.Linear
        };
    }
}
