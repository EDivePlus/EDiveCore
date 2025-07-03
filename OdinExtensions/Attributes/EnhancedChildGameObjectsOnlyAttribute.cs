// Author: František Holubec
// Created: 03.07.2025

using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public class EnhancedChildGameObjectsOnlyAttribute : Attribute
    {
        public bool IncludeSelf = true;

        public EnhancedChildGameObjectsOnlyAttribute() { }
        public EnhancedChildGameObjectsOnlyAttribute(bool includeSelf)
        {
            IncludeSelf = includeSelf;
        }
    }
}
