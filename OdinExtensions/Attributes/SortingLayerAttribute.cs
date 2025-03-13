using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    [Conditional("UNITY_EDITOR")]
    public class SortingLayerAttribute : Attribute
    { }
}
