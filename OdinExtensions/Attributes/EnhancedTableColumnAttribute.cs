using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class EnhancedTableColumnAttribute : Attribute
    {
        public string DisplayName;

        public bool Resizable = true;

        public bool HasPreserveWidth => _preserveWidth.HasValue;
        private bool? _preserveWidth;
        public bool PreserveWidth
        {
            get => _preserveWidth ?? false;
            set => _preserveWidth = value;
        }

        public bool HasWidth => _width.HasValue;
        private int? _width;
        public int Width
        {
            get => _width ?? 0;
            set => _width = value;
        }

        public bool HasMinWidth => _minWidth.HasValue;
        private int? _minWidth;
        public int MinWidth
        {
            get => _minWidth ?? 0;
            set => _minWidth = value;
        }

        public EnhancedTableColumnAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        public EnhancedTableColumnAttribute(string displayName, int width) : this(width)
        {
            DisplayName = displayName;
        }

        public EnhancedTableColumnAttribute(int width)
        {
            _width = width;
            Resizable = false;
        }

        public EnhancedTableColumnAttribute() { }
    }
}
