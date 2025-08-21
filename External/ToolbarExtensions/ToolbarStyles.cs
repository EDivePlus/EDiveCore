using UnityEditor;
using UnityEngine;

namespace EDIVE.External.ToolbarExtensions
{
    public static class ToolbarStyles
    {
        public static readonly GUIStyle ToolbarButton = new(EditorStyles.toolbarButton)
        {
            fixedHeight = 20,
            padding = new RectOffset(2,2,2,2)
        };

        public static readonly GUIStyle ToolbarButtonBiggerIcon = new(EditorStyles.toolbarButton)
        {
            fixedHeight = 20,
            padding = new RectOffset(1,1,1,1)
        };

        public static readonly GUIStyle ToolbarDropdown = new(EditorStyles.toolbarPopup)
        {
            fixedHeight = 20,
            padding = new RectOffset(5,5,2,2)
        };
        public static readonly GUIStyle ToolbarLabelButton = new(EditorStyles.toolbarButton)
        {
            fixedHeight = 20,
            padding = new RectOffset(5,5,2,2)
        };

        public static readonly GUIStyle Toolbar = new(EditorStyles.toolbar)
        {
            fixedHeight = 20,
            padding = new RectOffset(1,1,1,1)
        };
    }
}
