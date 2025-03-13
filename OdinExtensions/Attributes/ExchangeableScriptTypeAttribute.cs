using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    [IncludeMyAttributes]
    [HideReferenceObjectPicker]
    public class ExchangeableScriptTypeAttribute : Attribute
    {
        public Type BaseType;

        public string OnBeforeScriptChanged;
        
        public string OnAfterScriptChanged;
        
        public bool HideDropdownLabel;
        
        public string BaseTypeGetter;
        
        public string CustomTypesGetter;
        
        public float Space;


        public bool HasShowInInlineEditors => _showInInlineEditors.HasValue;
        private bool? _showInInlineEditors;
        public bool ShowInInlineEditors
        {
            get => _showInInlineEditors ?? false;
            set => _showInInlineEditors = value;
        }

        public ExchangeableScriptTypeAttribute()
        {

        }

        public ExchangeableScriptTypeAttribute(Type baseType, bool hideDropdownLabel = false)
        {
            BaseType = baseType;
            HideDropdownLabel = hideDropdownLabel;
        }
        
        public ExchangeableScriptTypeAttribute(string memberName, bool isCustomTypesList = false, bool hideDropdownLabel = false)
        {
            if (isCustomTypesList)
            {
                CustomTypesGetter = memberName;
            }
            else
            {
                BaseTypeGetter = memberName;
            }
            HideDropdownLabel = hideDropdownLabel;
        }
    }
}
