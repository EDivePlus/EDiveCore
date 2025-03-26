using System;
using System.Diagnostics;

namespace EDIVE.Utils.Json
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class JsonFieldAttribute : Attribute
    {
        public int PreviewLines = 3;
        public int MaxPreviewCharacters = 1000;
    }
}
