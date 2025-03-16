using System;
using System.Diagnostics;

namespace EDIVE.Utils.Json
{
    [Conditional("UNITY_EDITOR")]
    public class JsonFieldAttribute : Attribute
    {
        public int PreviewLines = 3;
        public int MaxPreviewCharacters = 1000;
    }
}
