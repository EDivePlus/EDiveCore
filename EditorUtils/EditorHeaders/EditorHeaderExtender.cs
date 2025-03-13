#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils.EditorHeaders
{
    public static class EditorHeaderExtender
    {
        private static GUIStyle _iconButtonStyle;
        public static GUIStyle IconButtonStyle => _iconButtonStyle ??= new GUIStyle(EditorStyles.iconButton)
        {
            padding = new RectOffset(2,2,2,2)
        };

        private static IList _headerItemsMethods;
        public static int HeaderElementsCount => _headerItemsMethods?.Count ?? 0;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update += RefreshHeaders;
        }

        private static void RefreshHeaders()
        {
            var fieldInfo = typeof(EditorGUIUtility).GetField("s_EditorHeaderItemsMethods", BindingFlags.NonPublic | BindingFlags.Static);
            _headerItemsMethods = (IList) fieldInfo?.GetValue(null);
            if (_headerItemsMethods == null)
                return;

            // Remove help icon button
            for (var index = _headerItemsMethods.Count - 1; index >= 0; index--)
            {
                var headerItem = (Delegate) _headerItemsMethods[index];
                if (headerItem.Method.Name == "HelpIconButton")
                    _headerItemsMethods.RemoveAt(index);
            }

            var delegateType = _headerItemsMethods.GetType().GetGenericArguments()[0];
            var currentHeaderElements = TypeCache.GetMethodsWithAttribute<CustomEditorHeaderItemAttribute>()
                .Where(m => m.IsStatic)
                .Select(m => new HeaderElement(m, m.GetCustomAttribute<CustomEditorHeaderItemAttribute>()))
                .OrderBy(m => m.Attribute.Priority);

            var signatureInfo = typeof(CustomEditorHeaderItemAttribute.MethodSignature).GetMethod("Invoke");
            foreach (var element in currentHeaderElements)
            {
                if (!AreSignaturesMatching(signatureInfo, element.Method))
                {
                    Debug.LogError($"{MethodToString(element.Method)} does not match {nameof(CustomEditorHeaderItemAttribute)} expected signature.\nUse {MethodToString(signatureInfo)}");
                    continue;
                }

                _headerItemsMethods.Add(Delegate.CreateDelegate(delegateType, element.Method));
            }
            EditorApplication.update -= RefreshHeaders;
        }

        private static string MethodToString(MethodInfo method)
        {
            return $"{(method.IsStatic ? "static " : "")}{method}";
        }

        private static bool AreSignaturesMatching(MethodInfo left, MethodInfo right)
        {
            if (left.ReturnType != right.ReturnType)
                return false;

            var parameters1 = left.GetParameters();
            var parameters2 = right.GetParameters();
            if (parameters1.Length != parameters2.Length)
                return false;

            for (var index = 0; index < parameters1.Length; index++)
            {
                if (parameters1[index].ParameterType != parameters2[index].ParameterType)
                    return false;
            }
            return true;
        }

        private class HeaderElement
        {
            public MethodInfo Method { get; }
            public CustomEditorHeaderItemAttribute Attribute { get; }

            public HeaderElement(MethodInfo method, CustomEditorHeaderItemAttribute attribute)
            {
                Method = method;
                Attribute = attribute;
            }
        }
    }
}
#endif
