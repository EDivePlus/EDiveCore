using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.OdinExtensions.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public class EnhancedFoldoutGroupAttribute : FoldoutGroupAttribute
    {
        public string UseIf;

        public bool Bold;
        public float SpaceBefore = 0;
        public float SpaceAfter = 0;
        
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

        public EnhancedFoldoutGroupAttribute(string group, float r, float g, float b, float a = 1f, bool expanded = false, int order = 0)
            : base(group, expanded, order)
        {
            DefaultColor= new Color(r, g, b, a);
            HasColorDefined = true;
        }

        public EnhancedFoldoutGroupAttribute(string group, string getColor, bool expanded = false, int order = 0)
            : base(group, expanded, order)
        {
            Color = getColor;
        }

        public EnhancedFoldoutGroupAttribute(string group, bool expanded = false, int order = 0)
            : base(group, expanded, order)
        {
        }

        public EnhancedFoldoutGroupAttribute()
            : this("_DefaultFoldoutGroup")
        {
        }
        
        protected override void CombineValuesWith(PropertyGroupAttribute other)
        {
            if (other is not EnhancedFoldoutGroupAttribute attr)
                return;

            if (attr.HasDefinedExpanded)
                Expanded = attr.Expanded;

            if (HasDefinedExpanded)
                attr.Expanded = Expanded;

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

            Bold = attr.Bold = Bold || attr.Bold;
            SpaceBefore = attr.SpaceBefore = Mathf.Max(SpaceBefore, attr.SpaceBefore);
            SpaceAfter = attr.SpaceAfter = Mathf.Max(SpaceAfter, attr.SpaceAfter);
        }
    }
}
