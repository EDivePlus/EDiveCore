// Author: Michal Petr
// Created: 19.10.2026

using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    // List of ScriptableObjects stored as sub-assets of the edited asset.
    // Add creates a sub-asset, remove deletes it. Elements are edited inline.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class SubAssetListAttribute : Attribute { }
}
