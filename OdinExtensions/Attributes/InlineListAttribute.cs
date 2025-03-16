// Author: František Holubec
// Created: 16.03.2025

using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    [Conditional("UNITY_EDITOR")]
    [DontApplyToListElements]
    public class InlineListAttribute : Attribute
    {
        public string ElementSuffixGetter;

        public InlineListAttribute() { }
    }
}
