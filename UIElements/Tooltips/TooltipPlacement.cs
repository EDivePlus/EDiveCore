// Author: Michal Petr
// Created: 09.03.2026

namespace EDIVE.UIElements.Tooltips
{
    public enum TooltipPlacement
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }
    
    public static class TooltipPlacementExtensions
    {
        public static bool IsTop(this TooltipPlacement placement)
        {
            return placement is TooltipPlacement.TopLeft or TooltipPlacement.TopCenter or TooltipPlacement.TopRight;
        }
        
        public static bool IsBottom(this TooltipPlacement placement)
        {
            return placement is TooltipPlacement.BottomLeft or TooltipPlacement.BottomCenter or TooltipPlacement.BottomRight;
        }
        
        public static bool IsLeft(this TooltipPlacement placement)
        {
            return placement is TooltipPlacement.TopLeft or TooltipPlacement.BottomLeft;
        }
        
        public static bool IsCenter(this TooltipPlacement placement)
        {
            return placement is TooltipPlacement.TopCenter or TooltipPlacement.BottomCenter;
        }
        
        public static bool IsRight(this TooltipPlacement placement)
        {
            return placement is TooltipPlacement.TopRight or TooltipPlacement.BottomRight;
        }
    }
}
