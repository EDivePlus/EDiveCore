// Author: František Holubec
// Created: 07.06.2026

#if UNITY_EDITOR
using EDIVE.OdinExtensions;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.DataStructures.Identifiers
{
    [UsedImplicitly]
    public sealed class UGuidDrawer : OdinValueDrawer<UGuid>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var current = ValueEntry.SmartValue;

            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

            EditorGUI.BeginChangeCheck();
            var hexString = SirenixEditorFields.DelayedTextField(current.HexString);
            if (EditorGUI.EndChangeCheck() && UGuid.TryParse(hexString, out var parsed))
            {
                ValueEntry.SmartValue = parsed;
                ValueEntry.ApplyChanges();
            }

            GUILayout.Space(2);
            var clearRect = GUILayoutUtility.GetRect(16, 16, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(16));
            if (SirenixEditorGUI.IconButton(clearRect, FontAwesomeEditorIcons.BroomWideSolid, "Clear"))
            {
                ValueEntry.SmartValue = UGuid.Empty;
                ValueEntry.ApplyChanges();
            }

            var generateRect = GUILayoutUtility.GetRect(16, 16, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(16));
            if (SirenixEditorGUI.IconButton(generateRect, FontAwesomeEditorIcons.ArrowsRotateRegular, "Generate new"))
            {
                ValueEntry.SmartValue = UGuid.New();
                ValueEntry.ApplyChanges();
            }

            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }
    }
}
#endif
