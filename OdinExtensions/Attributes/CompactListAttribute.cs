// Author: František Holubec
// Created: 08.06.2026

using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    [Conditional("UNITY_EDITOR")]
    [DontApplyToListElements]
    public class CompactListAttribute : Attribute
    {
        public bool Draggable = true;

        public bool HideRemoveButton;

        public bool HideAddButton;

        public bool ShowIndexLabels;

        public bool IsReadOnly;
        
        public int MaxItems = 20;
    }
}
