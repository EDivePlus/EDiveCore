// Author: František Holubec
// Created: 23.06.2025

using EnhancedUI.EnhancedScroller;

namespace EDIVE.StagePlay.Script.UI
{
    public abstract class AScriptSegmentDisplay : EnhancedScrollerCellView
    {
        public abstract void SetData(AScriptSegment data, StagePlayConfig config);
    }

    public abstract class AScriptSegmentDisplay<TData> : AScriptSegmentDisplay where TData : AScriptSegment
    {
        public TData Data { get; private set; }

        public override void SetData(AScriptSegment data, StagePlayConfig config)
        {
            if (data is not TData typedData)
                return;

            SetData(typedData, config);
        }

        protected virtual void SetData(TData data, StagePlayConfig config)
        {
            Data = data;
        }
    }
}
