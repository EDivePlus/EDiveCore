using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public class ShowAsStringAttribute : Attribute
    {
        public bool Overflow;
        public bool Nicify;
        public bool RichText;
        public bool Bold;
        
        public ShowAsStringAttribute()
        {
            Overflow = true;
        }
    }
}
