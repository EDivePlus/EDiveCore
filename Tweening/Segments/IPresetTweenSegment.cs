using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Tweening.Segments
{
    public interface IPresetTweenSegment : ITweenSegment
    {
#if UNITY_EDITOR
        bool TryConvertToDirectSegment(out IDirectTweenSegment result, IDictionary<TweenObjectReference, Object> targets);
#endif
    }
}
