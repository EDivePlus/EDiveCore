// Author: František Holubec
// Created: 03.06.2026

using System;

namespace EDIVE.Utils.SystemInformation
{
    public class SystemInfoEntry : IComparable<SystemInfoEntry>
    {
        public string Name { get; }
        public int Order { get; }
        private readonly Func<string> _valueGetter;

        public SystemInfoEntry(string name, string value, int order = 0) : this(name, () => value, order) { }
        public SystemInfoEntry(string name, Func<string> valueGetter, int order = 0)
        {
            Name = name;
            Order = order;
            _valueGetter = valueGetter;
        }
        
        public string GetValue()
        {
            try
            {
                return _valueGetter();
            }
            catch (Exception e)
            {
                return $"Error ({e.GetType().Name})";
            }
        }

        public int CompareTo(SystemInfoEntry other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var orderComparison = Order.CompareTo(other.Order);
            if (orderComparison != 0) return orderComparison;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }
}
