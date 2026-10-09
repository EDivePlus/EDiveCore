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

        public bool Bold;
        public bool HideGroupTitle;
        public float TitleWidth;
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

        // Odin keeps the first member's attribute and merges the others into it, unmerged settings only work on that first member
        protected override void CombineValuesWith(PropertyGroupAttribute other)
        {
            // Odin merges LabelText
            base.CombineValuesWith(other);

            if (other is not EnhancedBoxGroupAttribute attr)
                return;

            if (!ShowLabel || !attr.ShowLabel)
            {
                ShowLabel = false;
                attr.ShowLabel = false;
            }

            CenterLabel |= attr.CenterLabel;

            // Color string and RGB DefaultColor move together
            if (attr.HasColorDefined)
            {
                DefaultColor = attr.DefaultColor;
                Color = attr.Color;
            }

            if (HasColorDefined)
            {
                attr.Color = Color;
                attr.DefaultColor = DefaultColor;
            }

            UseIf = attr.UseIf = UseIf ?? attr.UseIf;
            Alignment = attr.Alignment = Alignment != ContentAlignment.Left ? Alignment : attr.Alignment;
            Bold = attr.Bold = Bold || attr.Bold;
            HideGroupTitle = attr.HideGroupTitle = HideGroupTitle || attr.HideGroupTitle;
            TitleWidth = attr.TitleWidth = Mathf.Max(TitleWidth, attr.TitleWidth);
            SpaceBefore = attr.SpaceBefore = Mathf.Max(SpaceBefore, attr.SpaceBefore);
            SpaceAfter = attr.SpaceAfter = Mathf.Max(SpaceAfter, attr.SpaceAfter);
        }
    }
}
