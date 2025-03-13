using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public sealed class EnhancedAssetListAttribute : Attribute
    {
        public string AssetNamePrefix;
        public bool AutoPopulate;
        public string CustomFilterMethod;
        public string LayerNames;
        public string Path;
        public string Tags;
        public bool ShowInlineEditor;
    }
}
