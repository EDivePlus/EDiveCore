// Author: František Holubec
// Created: 12.03.2026

using System;
using System.Reflection;
using UnityEditor;

namespace EDIVE.EditorUtils
{
    public static class CustomEditorUtility
    {
        private static readonly FieldInfo INSPECTED_TYPE_FIELD;
        private static readonly FieldInfo ALLOW_CHILDREN_FIELD;

        static CustomEditorUtility()
        {
            var customEditorType = typeof(CustomEditor);
            INSPECTED_TYPE_FIELD = customEditorType.GetField("m_InspectedType", BindingFlags.NonPublic | BindingFlags.Instance);
            ALLOW_CHILDREN_FIELD = customEditorType.GetField("m_EditorForChildClasses", BindingFlags.NonPublic | BindingFlags.Instance);

            if (INSPECTED_TYPE_FIELD == null || ALLOW_CHILDREN_FIELD == null)
                throw new Exception("Unity CustomEditor internals changed.");
        }

        public static Type GetCustomEditorType(Type inspectedType, Type excludeEditorType = null)
        {
            Type bestMatch = null;
            Type bestTarget = null;

            foreach (var editorType in TypeCache.GetTypesWithAttribute<CustomEditor>())
            {
                if (editorType.IsAbstract || editorType == excludeEditorType)
                    continue;

                var attr = editorType.GetCustomAttribute<CustomEditor>();

                var target = (Type)INSPECTED_TYPE_FIELD.GetValue(attr);
                var allowChildren = (bool)ALLOW_CHILDREN_FIELD.GetValue(attr);

                if (target != inspectedType && (!allowChildren || !target.IsAssignableFrom(inspectedType)))
                    continue;

                if (target == inspectedType)
                    return editorType;

                if (bestTarget == null || bestTarget.IsAssignableFrom(target))
                {
                    bestMatch = editorType;
                    bestTarget = target;
                }
            }

            return bestMatch;
        }
    }
}
