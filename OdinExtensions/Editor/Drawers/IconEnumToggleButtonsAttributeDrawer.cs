// Author: Michal Petr
// Created: 22.09.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class IconEnumToggleButtonsAttributeDrawer<T> : OdinAttributeDrawer<IconEnumToggleButtonsAttribute, T> where T : struct, Enum
    {
        private T[] _values;
        private GUIContent[] _contents;

        protected override void Initialize()
        {
            _values = EnumToggleButtonsGUI.GetValues<T>();
            _contents = EnumToggleButtonsGUI.GetContents(typeof(T));
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect();
            if (label != null)
                rect = EditorGUI.PrefixLabel(rect, label);

            var index = EnumToggleButtonsGUI.Draw(rect, Array.IndexOf(_values, ValueEntry.SmartValue), _contents);
            if (index >= 0)
                ValueEntry.SmartValue = _values[index];
        }
    }
}
