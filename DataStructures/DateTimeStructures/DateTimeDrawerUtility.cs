#if UNITY_EDITOR
using System;
using System.Linq;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.DataStructures.DateTimeStructures
{
    public static class DateTimeDrawerUtility
    {
        public static EditorIcon GetIcon(this DateTimeKind kind) => kind switch
        {
            DateTimeKind.Local => FontAwesomeEditorIcons.LocationDotSolid,
            DateTimeKind.Unspecified => FontAwesomeEditorIcons.ClockSolid,
            DateTimeKind.Utc => FontAwesomeEditorIcons.GlobeSolid,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        public static string GetDescription(this DateTimeKind kind) => kind switch
        {
            DateTimeKind.Local => $"Local - UTC +{TimeZoneInfo.Local.BaseUtcOffset:hh\\:mm}",
            DateTimeKind.Unspecified => "Generic - No time zone information",
            DateTimeKind.Utc => "UTC - Coordinated Universal Time",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        public static string GetLabel(this DateTimeKind kind) => kind switch
        {
            DateTimeKind.Local => "Local",
            DateTimeKind.Unspecified => "Generic",
            DateTimeKind.Utc => "UTC",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
        
        public static void DrawDateTimeField(GUIContent label, DateTime currentValue, Action<DateTime> valueSetter)
        {
            var day = currentValue.Day;
            var month = currentValue.Month;
            var year = currentValue.Year;
            var hour = currentValue.Hour;
            var min = currentValue.Minute;

            var propertyRect = SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            var kindRect = GUILayoutUtility.GetRect(16, 16, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(16));
            if (SirenixEditorGUI.IconButton(kindRect, currentValue.Kind.GetIcon(), currentValue.Kind.GetDescription()))
            {
                var selector = CreateDateTimeKindSelector(currentValue.Kind, newKind =>
                {
                    valueSetter?.Invoke(currentValue.WithKind(newKind));
                });
                selector.ShowInPopup(propertyRect);
            }

            EditorGUI.BeginChangeCheck();

            // Date
            GUILayout.Space(2);
            day = SirenixEditorFields.IntField(day, GUILayout.MinWidth(35));
            GUI.Label(GUILayoutUtility.GetLastRect().HorizontalPadding(0.0f, 2f), "D", SirenixGUIStyles.RightAlignedGreyMiniLabel);
            month = SirenixEditorFields.IntField(month, GUILayout.MinWidth(35));
            GUI.Label(GUILayoutUtility.GetLastRect().HorizontalPadding(0.0f, 2f), "M", SirenixGUIStyles.RightAlignedGreyMiniLabel);
            year = SirenixEditorFields.IntField(year, GUILayout.MinWidth(45));
            GUI.Label(GUILayoutUtility.GetLastRect().HorizontalPadding(0.0f, 2f), "Y", SirenixGUIStyles.RightAlignedGreyMiniLabel);

            EditorGUILayout.LabelField("—", GUILayout.Width(5));

            // Time
            hour = SirenixEditorFields.IntField(hour, GUILayout.MinWidth(35));
            GUI.Label(GUILayoutUtility.GetLastRect().HorizontalPadding(0.0f, 2f), "h", SirenixGUIStyles.RightAlignedGreyMiniLabel);
            min = SirenixEditorFields.IntField(min, GUILayout.MinWidth(35));
            GUI.Label(GUILayoutUtility.GetLastRect().HorizontalPadding(0.0f, 2f), "m", SirenixGUIStyles.RightAlignedGreyMiniLabel);

            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    valueSetter?.Invoke(new DateTime(year, month, day, hour, min, 0));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            // Picker
            GUILayout.Space(2);
            var pickerRect = GUILayoutUtility.GetRect(16, 16, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(16));
            if (SirenixEditorGUI.IconButton(pickerRect, FontAwesomeEditorIcons.CalendarPenSolid))
            {
                var dateTimePicker = new DateTimePicker(currentValue, dateTime =>
                {
                    valueSetter?.Invoke(dateTime);
                });
                dateTimePicker.ShowInPopup();
            }

            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        public static GenericSelector<DateTimeKind> CreateDateTimeKindSelector(DateTimeKind currentValue, Action<DateTimeKind> valueSetter)
        {
            var types = Enum.GetValues(typeof(DateTimeKind)).Cast<DateTimeKind>();
            var selector = new GenericSelector<DateTimeKind>(null, types, false);
            selector.SetSelection(currentValue);
            selector.SelectionTree.Config.DrawSearchToolbar = false;
            selector.SelectionTree.DefaultMenuStyle = new OdinMenuStyle
            {
                Offset = 6,
                IconPadding = 4
            };

            foreach (var menuItem in selector.SelectionTree.EnumerateTree())
            {
                var kind = (DateTimeKind) menuItem.Value;
                menuItem.Name = kind.GetDescription();
                menuItem.Icon = kind.GetIcon().Highlighted;
            }
            selector.SelectionConfirmed += t =>
            {
                var type = t.FirstOrDefault();
                valueSetter?.Invoke(type);
            };
            selector.EnableSingleClickToSelect();
            return selector;
        }
    }
}
#endif