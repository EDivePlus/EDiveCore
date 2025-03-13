using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public class IconButtonAttribute : ShowInInspectorAttribute
    {
        public EditorIconsBundle? Bundle;
        public string IconName;
        public string Tooltip;
        public bool DrawAsButton;
        
        public bool ShowLabel = true;

        public float Size
        {
            get => _size;
            set
            {
                _size = value;
                HasDefinedSize = true;
            }
        }

        private float _size;
        public bool HasDefinedSize;

        private string _label;
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                DrawAsButton = true;
            }
        }

        public IconButtonAttribute(string label, EditorIconsBundle bundle, string iconName, string tooltip = "") : this(label, iconName, tooltip)
        {
            Bundle = bundle;
        }

        public IconButtonAttribute(string label, string iconName, string tooltip = "") : this(iconName, tooltip)
        {
            Label = label;
        }

        public IconButtonAttribute(string label, FontAwesomeEditorIconType iconType, string tooltip = "") : this(iconType, tooltip)
        {
            Label = label;
        }

        public IconButtonAttribute(EditorIconsBundle bundle, string iconName, string tooltip = "") : this(iconName, tooltip)
        {
            Bundle = bundle;
        }

        public IconButtonAttribute(string iconName, string tooltip = "")
        {
            IconName = iconName;
            Tooltip = tooltip;
        }

        public IconButtonAttribute(FontAwesomeEditorIconType iconType, string tooltip = "") : this(EditorIconsBundle.FontAwesome, iconType.ToString(), tooltip)
        {
            
        }
    }
}
