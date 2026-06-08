// Author: František Holubec
// Created: 08.06.2026

using EDIVE.StateHandling.StateValuePresets;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.StateHandling.Editor
{
    [UsedImplicitly]
    public class StateValuePresetContextMenuDrawer : OdinValueDrawer<AStateValuePreset>, IDefinesGenericMenuItems
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);
        }

        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
        {
            var record = property.Parent?.Parent?.ValueEntry?.WeakSmartValue as ObjectStatePresetRecord;
            if (property.ValueEntry.WeakSmartValue is not AStateValuePreset preset || record?.Target == null)
                return;

            var target = record.Target;

            genericMenu.AddSeparator("");
            genericMenu.AddItem(new GUIContent("Apply"), false, () =>
            {
                Undo.RecordObject(target, "Apply state preset");
                preset.ApplyTo(target);
                EditorUtility.SetDirty(target);
            });

            genericMenu.AddItem(new GUIContent("Capture"), false, () =>
            {
                preset.CaptureFrom(target);
                property.MarkSerializationRootDirty();
            });
        }
    }
}