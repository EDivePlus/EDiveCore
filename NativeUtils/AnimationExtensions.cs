using System.Linq;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class AnimationExtensions
    {
        public static void FlipY(this AnimationCurve curve)
        {
            var minY = curve.keys.Min(key => key.value);
            var maxY = curve.keys.Max(key => key.value);

            curve.keys = curve.keys
                .Select(key => new Keyframe(key.time, maxY - (key.value - minY), -key.inTangent, -key.outTangent))
                .ToArray();
        }

        public static void FlipX(this AnimationCurve curve)
        {
            var minX = curve.keys.Min(key => key.time);
            var maxX = curve.keys.Max(key => key.time);

            curve.keys = curve.keys
                .Select(key => new Keyframe(maxX - (key.time - minX), key.value, -key.inTangent, -key.outTangent))
                .ToArray();
        }

        public static void FlipXY(this AnimationCurve curve)
        {
            var minY = curve.keys.Min(key => key.value);
            var maxY = curve.keys.Max(key => key.value);
            var minX = curve.keys.Min(key => key.time);
            var maxX = curve.keys.Max(key => key.time);

            curve.keys = curve.keys
                .Select(key => new Keyframe(maxX - (key.time - minX), maxY - (key.value - minY), key.inTangent, key.outTangent))
                .ToArray();
        }
    }
}
