using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Tweening.Segments
{
    public interface IDirectTweenSegment : ITweenSegment
    {
#if UNITY_EDITOR
        bool TryConvertToPresetSegment(out IPresetTweenSegment result, IDictionary<Object, TweenObjectReference> references);
#endif
    }
}
