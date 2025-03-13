using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public class EnhancedBoxGroupAttribute : BoxGroupAttribute
    {
        public string UseIf;

        public float SpaceBefore = 0;
        public float SpaceAfter = 0;

        public Color DefaultColor { get; private set; }

        public ContentAlignment Alignment;
        
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

        
        public EnhancedBoxGroupAttribute(string group, float r, float g, float b, float a = 1f, bool showLabel = true, bool centerLabel = false, int order = 0) 
            : base (group, showLabel, centerLabel, order)
        {
            DefaultColor = new Color(r, g, b, a);
            HasColorDefined = true;
        }
        
        public EnhancedBoxGroupAttribute(string group, string color, bool showLabel = true, bool centerLabel = false, int order = 0) 
            : base (group, showLabel, centerLabel, order)
        {
            Color = color;
        }

        public EnhancedBoxGroupAttribute(string group, bool showLabel = true, bool centerLabel = false, float order = 0.0f)
            : base(group, showLabel, centerLabel, order)
        {
        }
        
        public EnhancedBoxGroupAttribute() : base()
        {
        }

        protected override void CombineValuesWith(PropertyGroupAttribute other)
        {
            if (other is EnhancedBoxGroupAttribute boxGroupAttribute)
            {
                if (!ShowLabel || !boxGroupAttribute.ShowLabel)
                {
                    ShowLabel = false;
                    boxGroupAttribute.ShowLabel = false;
                }

                CenterLabel |= boxGroupAttribute.CenterLabel;
                
                if (boxGroupAttribute.HasColorDefined) 
                    Color = boxGroupAttribute.Color;
                
                if (HasColorDefined) 
                    boxGroupAttribute.Color = Color;
                
                SpaceBefore = boxGroupAttribute.SpaceBefore = Mathf.Max(SpaceBefore, boxGroupAttribute.SpaceBefore);
                SpaceAfter = boxGroupAttribute.SpaceAfter = Mathf.Max(SpaceAfter, boxGroupAttribute.SpaceAfter);
            }
        }
    }
}
