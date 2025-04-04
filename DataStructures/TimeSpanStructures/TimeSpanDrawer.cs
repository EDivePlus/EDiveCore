#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.DataStructures.TimeSpanStructures
{
    [UsedImplicitly]
    public sealed class UTimeSpanDrawer : OdinValueDrawer<UTimeSpan>
    {
        private TimeSpanDrawerSettingsAttribute _settingsAttribute;

        protected override void Initialize()
        {
            base.Initialize();
            _settingsAttribute = Property.GetAttribute<TimeSpanDrawerSettingsAttribute>() ?? new TimeSpanDrawerSettingsAttribute();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var value = TimeDrawerUtils.DrawTimeSpanField(label, ValueEntry.SmartValue.Value, _settingsAttribute.HighestUnit, _settingsAttribute.LowestUnit);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue.Value = value;
                ValueEntry.ApplyChanges();
                Property.MarkSerializationRootDirty();
            }
        }
    }

    [UsedImplicitly]
    public sealed class TimeSpanDrawer : OdinValueDrawer<TimeSpan>
    {
        private TimeSpanDrawerSettingsAttribute _settingsAttribute;

        protected override void Initialize()
        {
            base.Initialize();
            _settingsAttribute = Property.GetAttribute<TimeSpanDrawerSettingsAttribute>() ?? new TimeSpanDrawerSettingsAttribute();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var value = TimeDrawerUtils.DrawTimeSpanField(label, ValueEntry.SmartValue, _settingsAttribute.HighestUnit, _settingsAttribute.LowestUnit);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue = value;
                ValueEntry.ApplyChanges();
                Property.MarkSerializationRootDirty();
            }
        }
    }
}
#endif
