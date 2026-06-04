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
        public int ContentIndent = 1;
        
        public string OnTypeChanged;

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
        public EnhancedTypeSelectorAttribute(Type baseType, bool hideFoldout = false, bool hideDropdownLabel = false) : this(hideFoldout, hideDropdownLabel)
        {
            BaseType = baseType;
        }
    }
}
