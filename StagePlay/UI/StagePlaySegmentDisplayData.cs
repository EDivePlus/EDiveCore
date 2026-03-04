// Author: František Holubec
// Created: 18.02.2026

namespace EDIVE.StagePlay.UI
{
    public class StagePlaySegmentDisplayData
    {        
        public int Index { get; }
        public StagePlaySegment Segment { get; }
        public StagePlayState SharedData { get; }
        
        public StagePlaySegmentDisplayData(int index, StagePlaySegment segment, StagePlayState sharedData)
        {
            Index = index;
            Segment = segment;
            SharedData = sharedData;
        }
    }
}
