#if UNITY_EDITOR
using System;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.DataStructures.TimeSpanStructures
{
    public static class TimeDrawerUtils
    {
        private static readonly TimeUnit[] ORDERED_UNITS =
        {
            TimeUnit.Days,
            TimeUnit.Hours,
            TimeUnit.Minutes,
            TimeUnit.Seconds,
            TimeUnit.Milliseconds
        };

        public static System.TimeSpan DrawTimeSpanField(GUIContent label, System.TimeSpan currentValue, TimeUnit highestUnit = TimeUnit.Days, TimeUnit lowestUnit = TimeUnit.Seconds)
        {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

            if (lowestUnit == highestUnit)
            {
                currentValue = DrawTimeUnitSingleField(currentValue, lowestUnit);
            }
            else
            {
                foreach (var unit in ORDERED_UNITS)
                {
                    if (highestUnit < unit || unit < lowestUnit)
                        continue;

                    if (unit == lowestUnit) currentValue = DrawTimeUnitLowestField(currentValue, unit);
                    else if (unit == highestUnit) currentValue = DrawTimeUnitHighestField(currentValue, unit);
                    else currentValue = DrawTimeUnitField(currentValue, unit);
                }
            }
            SirenixEditorGUI.EndHorizontalPropertyLayout();
            return currentValue;
        }

        private static System.TimeSpan DrawTimeUnitField(System.TimeSpan currentValue, TimeUnit unit, GetUnitValueDelegate getter, WithUnitValueDelegate setter)
        {
            var value = getter.Invoke(currentValue, unit);
            var newValue = EditorGUILayout.FloatField((float) value);

            var prefixLabel = unit.ToShortString();
            GUI.Label(GUILayoutUtility.GetLastRect().HorizontalPadding(0.0f, 2f), prefixLabel, SirenixGUIStyles.RightAlignedGreyMiniLabel);

            return Math.Abs(newValue - value) > 0.0001 ? setter.Invoke(currentValue, unit, newValue) : currentValue;
        }

        private static System.TimeSpan DrawTimeUnitField(System.TimeSpan currentValue, TimeUnit unit) =>
            DrawTimeUnitField(currentValue, unit, TimeUnitExtensions.GetUnitValue, TimeUnitExtensions.WithUnitValue);

        private static System.TimeSpan DrawTimeUnitLowestField(System.TimeSpan currentValue, TimeUnit unit) =>
            DrawTimeUnitField(currentValue, unit, TimeUnitExtensions.GetLowestUnitValue, TimeUnitExtensions.WithLowestUnitValue);

        private static System.TimeSpan DrawTimeUnitHighestField(System.TimeSpan currentValue, TimeUnit unit) =>
            DrawTimeUnitField(currentValue, unit, TimeUnitExtensions.GetHighestUnitValue, TimeUnitExtensions.WithHighestUnitValue);

        private static System.TimeSpan DrawTimeUnitSingleField(System.TimeSpan currentValue, TimeUnit unit) =>
            DrawTimeUnitField(currentValue, unit, TimeUnitExtensions.GetSingleUnitValue, TimeUnitExtensions.FromSingleUnitValue);

        public static System.TimeSpan DrawTimeSpanSlider(System.TimeSpan value, System.TimeSpan min, System.TimeSpan max, TimeUnit snapping)
        {
            var rect = EditorGUILayout.GetControlRect(false);
            var totalSeconds = GUI.HorizontalSlider(rect, (float) value.TotalSeconds, (float) min.TotalSeconds, (float) max.TotalSeconds);
            return System.TimeSpan.FromSeconds(totalSeconds).SnapToUnit(snapping);
        }

        public static Tuple<System.TimeSpan, System.TimeSpan> DrawTimeSpanMinMaxSlider(System.TimeSpan start, System.TimeSpan end, System.TimeSpan min, System.TimeSpan max, TimeUnit snapping)
        {
            var startSeconds = (float) start.TotalSeconds;
            var endSeconds = (float) end.TotalSeconds;

            EditorGUILayout.MinMaxSlider(ref startSeconds, ref endSeconds, (float) min.TotalSeconds, (float) max.TotalSeconds);

            start = System.TimeSpan.FromSeconds(startSeconds).SnapToUnit(snapping);
            end = System.TimeSpan.FromSeconds(endSeconds).SnapToUnit(snapping);

            return new Tuple<System.TimeSpan, System.TimeSpan>(start, end);
        }

    }
}
#endif
