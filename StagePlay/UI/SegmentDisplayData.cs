// Author: František Holubec
// Created: 18.02.2026

namespace EDIVE.StagePlay.UI
{
    public abstract class ASegmentDisplayData { }
    public class SegmentDisplayData<TSegment> : ASegmentDisplayData where TSegment : APlaySegment
    {        
        public int Index { get; }
        public TSegment Segment { get; }
        public StagePlayState SharedData { get; }
        
        public SegmentDisplayData(int index, TSegment segment, StagePlayState sharedData)
        {
            Index = index;
            Segment = segment;
            SharedData = sharedData;
        }
    }
}
