using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public class EnhancedObjectDrawerAttribute : Attribute
    {
        public string ElementCondition;
        public string PreferredTypeGetter;
        public bool PreferCurrentType = true;
        public bool ShowSelectRoot = true;

        public EnhancedObjectDrawerAttribute() { }
        public EnhancedObjectDrawerAttribute(string elementCondition)
        {
            ElementCondition = elementCondition;
        }
    }
}
