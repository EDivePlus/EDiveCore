// Author: František Holubec
// Created: 29.10.2026

using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;

namespace EDIVE.Localization.Editor
{
    // Unity's drawer needs a SerializedProperty, this one also works in plain Odin trees
    [UsedImplicitly]
    public sealed class LocaleIdentifierDrawer : OdinValueDrawer<LocaleIdentifier>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var codes = LocalizationEditorSettings.GetLocales().Select(l => l.Identifier.Code).ToList();
            var current = ValueEntry.SmartValue.Code;
            if (!string.IsNullOrEmpty(current) && !codes.Contains(current))
                codes.Add(current);

            var index = codes.IndexOf(current);
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup(label ?? GUIContent.none, index, codes.Select(c => new GUIContent(c)).ToArray());
            if (EditorGUI.EndChangeCheck() && index >= 0)
                ValueEntry.SmartValue = new LocaleIdentifier(codes[index]);
        }
    }
}
