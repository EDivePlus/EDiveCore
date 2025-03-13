using System;
using System.Diagnostics;
using UnityEngine;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public class EnhancedInlineEditorAttribute : Attribute
    {
        private bool _expanded; 
        public bool Expanded
        {
            get => _expanded;
            set
            {
                _expanded = value;
                HasDefinedExpanded = true;
            }
        }

        public bool HasDefinedExpanded { get; private set; }
        
        public float MaxHeight = 0f;
        
        public bool HideFrame;
        
        public ObjectFieldVisibilityMode ObjectFieldVisibility;
        public LabelVisibilityMode LabelVisibility;
        
        public int ContentIndent;
        
        public bool ReadOnlyContent;
        
        public string Condition;
        
        public Color DefaultColor { get; private set; }
        
        private string _color;

        public string Color
        {
            get => _color;
            set
            {
                _color = value;
                HasColorDefined = true;
            }
        }
        
        public bool HasColorDefined { get; private set; }
    }
    
    public enum ObjectFieldVisibilityMode
    {
        Shown,
        Hidden,
        ShowIfNull,
    }
    
    public enum LabelVisibilityMode
    {
        ShowIfObjectFieldVisible,
        Shown,
        Hidden,
    }
}
