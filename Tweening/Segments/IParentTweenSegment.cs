using System.Collections.Generic;

namespace EDIVE.Tweening.Segments
{
    public interface IParentTweenSegment
    {
        IEnumerable<ITweenSegment> GetChildSegments();
    }
}
