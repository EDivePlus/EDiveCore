using System;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public class EnhancedPreviewFieldAttribute : Attribute
    {
        public float Height = 60;
        public PreviewFieldAlignment Alignment = PreviewFieldAlignment.Right;
        public bool UseGameObject = true;

        public EnhancedPreviewFieldAttribute() { }

        public EnhancedPreviewFieldAttribute(float height)
        {
            Height = height;
        }

        public EnhancedPreviewFieldAttribute(float height, PreviewFieldAlignment alignment)
        {
            Height = height;
            Alignment = alignment;
        }

        public EnhancedPreviewFieldAttribute(PreviewFieldAlignment alignment)
        {
            Alignment = alignment;
        }
    }
    
    public enum PreviewFieldAlignment
    {
        Right,
        Left
    }
}
