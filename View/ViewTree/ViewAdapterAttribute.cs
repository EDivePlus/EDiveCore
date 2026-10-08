// Author: František Holubec
// Created: 08.10.2026

using System;

namespace EDIVE.View.ViewTree
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewAdapterAttribute : Attribute
    {
        public Type ComponentType;
        public bool Required;

        public ViewAdapterAttribute(Type componentType, bool required = true)
        {
            ComponentType = componentType;
            Required = required;
        }
    }
}
