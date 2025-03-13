#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using UnityEngine;

namespace EDIVE.EditorUtils.EditorHeaders
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CustomEditorHeaderItemAttribute : Attribute
    {
        public readonly int Priority;

        public CustomEditorHeaderItemAttribute(int priority = 1)
        {
            Priority = priority;
        }

        public delegate bool MethodSignature(Rect rectangle, UnityEngine.Object[] targets);
    }
}
#endif
