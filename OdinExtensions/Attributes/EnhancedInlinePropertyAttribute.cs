using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public class EnhancedInlinePropertyAttribute : Attribute
    {
        public bool HideLabel;
        public bool PushContentRight;
        public int ContentIndent = 1;
        
        public EnhancedInlinePropertyAttribute() {}
        public EnhancedInlinePropertyAttribute(bool hideLabel, int contentIndent = 1)
        {
            HideLabel = hideLabel;
            ContentIndent = contentIndent;
        }
    }
}
