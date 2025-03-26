// Author: František Holubec
// Created: 26.03.2025

using System;
using System.Diagnostics;

namespace EDIVE.Utils.Json
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class JsonAssetFieldAttribute : Attribute
    {
        public string Serializer;

        public JsonAssetFieldAttribute() { }

        public JsonAssetFieldAttribute(string serializer) { Serializer = serializer; }
    }
}
