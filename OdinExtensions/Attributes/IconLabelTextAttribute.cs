using System;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [DontApplyToListElements]
    [AttributeUsage(AttributeTargets.All)]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public class IconLabelTextAttribute : Attribute
    {
        public string Text;
        public bool NicifyText;
        public EditorIconsBundle? Bundle;
        public string IconName;
        public bool HideText;

        public EditorIconTextureType Type = EditorIconTextureType.Active;

        public IconLabelTextAttribute(string iconName, string text = null, bool nicifyText = false)
        {
            Text = text;
            NicifyText = nicifyText;
            IconName = iconName;
        }

        public IconLabelTextAttribute(EditorIconsBundle bundle, string iconName, string text = null, bool nicifyText = false) : this(iconName, text, nicifyText)
        {
            Bundle = bundle;
        }

        public IconLabelTextAttribute(FontAwesomeEditorIconType iconType, string text = null, bool nicifyText = false) : this(EditorIconsBundle.FontAwesome, iconType.ToString(), text, nicifyText)
        {

        }
    }
}
