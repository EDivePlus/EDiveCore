using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public sealed class EnhancedDictionaryAttribute : Attribute
    {
        public string KeyLabel = "Key";

        public string ValueLabel = "Value";

        public DictionaryDisplayOptions DisplayMode;

        public bool IsReadOnly;

        public float KeyColumnWidth = 130f;
        

        public string OnTitleBarGUI;

        public string CustomKeyDrawer;

        public string CustomValueDrawer;

        public bool ExpandedHasValue { get; private set; }

        public bool Expanded
        {
            get => _expanded;
            set
            {
                _expanded = value;
                ExpandedHasValue = true;
            }
        }
        
        private bool _expanded;
    }
}
