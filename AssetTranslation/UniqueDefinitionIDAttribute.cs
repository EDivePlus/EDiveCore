// Author: František Holubec
// Created: 14.04.2025

using System;
using System.Diagnostics;

namespace EDIVE.AssetTranslation
{
    [AttributeUsage(AttributeTargets.All)]
    [Conditional("UNITY_EDITOR")]
    public class UniqueDefinitionIDAttribute : Attribute
    {
        public string FormatFileNameMethod;

        public UniqueDefinitionIDAttribute() { }

        public UniqueDefinitionIDAttribute(string formatFileNameMethod)
        {
            FormatFileNameMethod = formatFileNameMethod;
        }
    }
}
