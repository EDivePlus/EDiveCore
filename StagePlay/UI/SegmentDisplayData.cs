// Author: František Holubec
// Created: 18.02.2026

namespace EDIVE.StagePlay.UI
{
    public abstract class ASegmentDisplayData { }
    public class SegmentDisplayData<TSegment> : ASegmentDisplayData where TSegment : AScriptSegment
    {        
        public int Index { get; }
        public TSegment Segment { get; }
        public SharedSegmentData SharedData { get; }
        
        public SegmentDisplayData(int index, TSegment segment, SharedSegmentData sharedData)
        {
            Index = index;
            Segment = segment;
            SharedData = sharedData;
        }
    }
}
