// Author: Michal Petr
// Created: 22.09.2026

using System;
using System.Linq;
using System.Reflection;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public static class EnumToggleButtonsGUI
    {
        // Declaration order, so the buttons can be arranged independently of the enum values
        private static FieldInfo[] GetMembers(Type enumType)
        {
            return enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        }

        public static T[] GetValues<T>() where T : struct, Enum
        {
            return GetMembers(typeof(T)).Select(field => (T) field.GetValue(null)).ToArray();
        }

        public static object[] GetValues(Type enumType)
        {
            return GetMembers(enumType).Select(field => field.GetValue(null)).ToArray();
        }

        public static GUIContent[] GetContents(Type enumType)
        {
            return GetMembers(enumType).Select(field =>
            {
                var name = ObjectNames.NicifyVariableName(field.Name);
                var attribute = field.GetCustomAttribute<IconLabelTextAttribute>();
                if (attribute == null)
                    return new GUIContent(name);

                var text = attribute.HideText ? string.Empty : attribute.Text ?? name;
                var texture = EditorIconsUtility.GetIcon(attribute.IconName, attribute.Bundle)?.GetTexture(attribute.Type);
                if (texture && text.Length > 0)
                    text = " " + text;
                return new GUIContent(text, texture, name);
            }).ToArray();
        }

        public static int Draw(Rect rect, int selectedIndex, GUIContent[] contents)
        {
            var width = rect.width / contents.Length;
            for (var i = 0; i < contents.Length; i++)
            {
                var style = i == 0 ? SirenixGUIStyles.ButtonLeft : i == contents.Length - 1 ? SirenixGUIStyles.ButtonRight : SirenixGUIStyles.ButtonMid;
                var buttonRect = new Rect(rect.x + i * width, rect.y, width, rect.height);
                var selected = i == selectedIndex;
                if (GUI.Toggle(buttonRect, selected, contents[i], style) && !selected)
                    selectedIndex = i;
            }
            return selectedIndex;
        }
    }
}
