// Author: František Holubec
// Created: 23.06.2025

using EnhancedUI.EnhancedScroller;

namespace EDIVE.StagePlay.UI
{
    public abstract class AScriptSegmentDisplay : EnhancedScrollerCellView
    {
        public abstract void SetData(AScriptSegment data);
    } 
    
    public abstract class AScriptSegmentDisplay<TData> : AScriptSegmentDisplay where TData : AScriptSegment
    {
        public TData Data { get; private set; }
        
        public sealed override void SetData(AScriptSegment data)
        {
            if (data is not TData typedData)
                return;
            
            SetData(typedData);
        }

        protected virtual void SetData(TData data)
        {
            Data = data;
        }
    }
}
