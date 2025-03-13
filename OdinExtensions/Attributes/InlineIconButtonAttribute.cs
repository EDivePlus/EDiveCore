using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class InlineIconButtonAttribute : Attribute
    {
        public EditorIconsBundle? Bundle;
        public string IconName;

        public string Action;
        public string Tooltip;
        public string ShowIf;
        public bool GUIAlwaysEnabled;

        public InlineIconButtonAttribute(EditorIconsBundle bundle, string iconName, string action, string tooltip = "") : this(iconName, action, tooltip)
        {
            Bundle = bundle;
        }

        public InlineIconButtonAttribute(string iconName, string action, string tooltip = "")
        {
            Action = action;
            IconName = iconName;
            Tooltip = tooltip;
        }

        public InlineIconButtonAttribute(FontAwesomeEditorIconType iconType, string action, string tooltip = "") : this(EditorIconsBundle.FontAwesome, iconType.ToString(), action, tooltip)
        {
            
        }
    }
}
