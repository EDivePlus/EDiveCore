using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    [IncludeMyAttributes]
    [HideReferenceObjectPicker]
    public class EnhancedTypeSelectorAttribute : Attribute
    {
        public Type BaseType;
        public string BaseTypeGetter;
        
        public string CustomTypesGetter;
        public bool HideDropdownLabel;
        public bool HideFoldout;

        public float Space;
        public int ContentIndent;
        
        public string OnTypeChanged;

        // Draws a box with the dropdown in its header, HideFoldout keeps it always open. BoxColor enables it too
        public bool Box;
        public string BoxColor;

        public bool UseBox => Box || BoxColor != null;

        // Closed generic types use TypeRegistryItem name and icon of their generic definition
        public bool UseGenericRegistryItems = true;

        public bool HasShowInInlineEditors => _showInInlineEditors.HasValue;
        private bool? _showInInlineEditors;
        public bool ShowInInlineEditors
        {
            get => _showInInlineEditors ?? false;
            set => _showInInlineEditors = value;
        }
        
        public EnhancedTypeSelectorAttribute() { }
        public EnhancedTypeSelectorAttribute(bool hideFoldout, bool hideDropdownLabel = false)
        {
                HideFoldout = hideFoldout;
                HideDropdownLabel = hideDropdownLabel;
        }
        public EnhancedTypeSelectorAttribute(bool hideFoldout, int contentIndent, bool hideDropdownLabel = false)
        {
            HideFoldout = hideFoldout;
            ContentIndent = contentIndent;
            HideDropdownLabel = hideDropdownLabel;
        }
        
        public EnhancedTypeSelectorAttribute(Type baseType, bool hideFoldout = false, bool hideDropdownLabel = false) : this(hideFoldout, hideDropdownLabel)
        {
            BaseType = baseType;
        }
    }
}
