using System.Linq;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class AnimationExtensions
    {
        public static void FlipY(this AnimationCurve curve) => Flip(curve, false, true);

        public static void FlipX(this AnimationCurve curve) => Flip(curve, true, false);

        public static void FlipXY(this AnimationCurve curve) => Flip(curve, true, true);

        // Mirrors within the curve bounds. Mirrored time swaps in and out sides, each axis flips the slope sign.
        private static void Flip(AnimationCurve curve, bool flipTime, bool flipValue)
        {
            var keys = curve.keys;
            if (keys.Length == 0)
                return;

            var timeSum = keys.Min(key => key.time) + keys.Max(key => key.time);
            var valueSum = keys.Min(key => key.value) + keys.Max(key => key.value);
            var slopeSign = flipTime == flipValue ? 1f : -1f;

            var flipped = keys.Select(key =>
            {
                var result = key;
                if (flipValue)
                    result.value = valueSum - key.value;

                if (flipTime)
                {
                    result.time = timeSum - key.time;
                    result.inTangent = key.outTangent;
                    result.outTangent = key.inTangent;
                    result.inWeight = key.outWeight;
                    result.outWeight = key.inWeight;
                    result.weightedMode = key.weightedMode switch
                    {
                        WeightedMode.In => WeightedMode.Out,
                        WeightedMode.Out => WeightedMode.In,
                        _ => key.weightedMode
                    };
                }

                result.inTangent *= slopeSign;
                result.outTangent *= slopeSign;
                return result;
            });

            curve.keys = flipped.OrderBy(key => key.time).ToArray();
        }
    }
}
