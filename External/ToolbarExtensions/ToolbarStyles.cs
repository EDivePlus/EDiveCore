using UnityEngine;

namespace EDIVE.External.ToolbarExtensions
{
    public static class ToolbarStyles
    {
        public static GUIStyle ToolbarButton = new("ToolbarButton")
        {
            fixedHeight = 18,
            padding = new RectOffset(2,2,2,2)
        };

        public static GUIStyle ToolbarButtonBiggerIcon = new("ToolbarButton")
        {
            fixedHeight = 18,
            padding = new RectOffset(1,1,1,1)
        };

        public static GUIStyle ToolbarLabelButton = new("ToolbarButton")
        {
            fixedHeight = 18,
            padding = new RectOffset(5,5,2,2)
        };

        public static GUIStyle Toolbar = new("Toolbar")
        {
            fixedHeight = 18,
            padding = new RectOffset(1,1,1,1)
        };
    }
}
