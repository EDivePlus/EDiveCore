// Author: Michal Petr
// Created: 22.09.2026

using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IconEnumToggleButtonsAttribute : Attribute
    {
    }
}
