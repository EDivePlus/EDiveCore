// Author: František Holubec
// Created: 06.03.2026

#if UNITY_6000_3_OR_NEWER
using System;
using System.Reflection;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace EDIVE.EditorUtils
{
    public static class MainToolbarElementFactory
    {
        private static readonly ConstructorInfo CTOR;

        static MainToolbarElementFactory()
        {
            var type = Type.GetType("UnityEditor.Toolbars.MainToolbarCustom, UnityEditor");
            CTOR = type?.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(Func<VisualElement>) }, null);
        }

        public static MainToolbarElement Create(Func<VisualElement> createElement)
        {
            return CTOR == null ? null : CTOR.Invoke(new object[] { createElement }) as MainToolbarElement;
        }
    }
}
#endif
