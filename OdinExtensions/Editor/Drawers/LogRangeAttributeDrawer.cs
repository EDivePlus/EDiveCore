using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public sealed class LogRangeAttributeFloatDrawer : OdinAttributeDrawer<LogRangeAttribute, float>
    {
        private LogRangeHelper _helper;

        protected override void Initialize()
        {
            _helper = new LogRangeHelper(Attribute, Property);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            _helper.DrawFloat(label, ValueEntry.SmartValue, result =>
            {
                ValueEntry.SmartValue = result;
            });
        }
    }

    public sealed class LogRangeAttributeDoubleDrawer : OdinAttributeDrawer<LogRangeAttribute, double>
    {
        private LogRangeHelper _helper;

        protected override void Initialize()
        {
            _helper = new LogRangeHelper(Attribute, Property);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var value = Mathf.Clamp((float) ValueEntry.SmartValue, float.MinValue, float.MaxValue);
            _helper.DrawFloat(label, value, result =>
            {
                ValueEntry.SmartValue = result;
            });
        }
    }

    public sealed class LogRangeAttributeIntDrawer : OdinAttributeDrawer<LogRangeAttribute, int>
    {
        private LogRangeHelper _helper;

        protected override void Initialize()
        {
            _helper = new LogRangeHelper(Attribute, Property);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            _helper.DrawInt(label, ValueEntry.SmartValue, result =>
            {
                ValueEntry.SmartValue = result;
            });
        }
    }

    public sealed class LogRangeAttributeUIntDrawer : OdinAttributeDrawer<LogRangeAttribute, uint>
    {
        private LogRangeHelper _helper;

        protected override void Initialize()
        {
            _helper = new LogRangeHelper(Attribute, Property);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            _helper.DrawInt(label, (int) ValueEntry.SmartValue, result =>
            {
                ValueEntry.SmartValue = (uint) result;
            }, 0);
        }
    }

    public class LogRangeHelper
    {
        private readonly ValueResolver<float> _getterMinValue;
        private readonly ValueResolver<float> _getterCenterValue;
        private readonly ValueResolver<float> _getterMaxValue;

        private readonly LogRangeAttribute _attribute;

        private float Min => _getterMinValue?.GetValue() ?? _attribute.Min;
        private float Center => _getterCenterValue?.GetValue() ?? _attribute.Center;
        private float Max => _getterMaxValue?.GetValue() ?? _attribute.Max;
        
        public LogRangeHelper(LogRangeAttribute attribute, InspectorProperty property)
        {
            _attribute = attribute;
            if (attribute.MinGetter != null) _getterMinValue = ValueResolver.Get<float>(property, attribute.MinGetter);
            if (attribute.CenterGetter != null) _getterCenterValue = ValueResolver.Get<float>(property, attribute.CenterGetter);
            if (attribute.MaxGetter != null) _getterMaxValue = ValueResolver.Get<float>(property, attribute.MaxGetter);
        }

        public void BeginDraw()
        {
            ValueResolver.DrawErrors(_getterMinValue, _getterCenterValue, _getterMaxValue);
        }

        public float DrawSlider(float value)
        {
            var rangeConverter = new LogRangeConverter(Min, Center, Max);
            var rect = EditorGUILayout.GetControlRect(false);
            var normalized = rangeConverter.ToNormalized(value);
            var logValue = GUI.HorizontalSlider(rect, normalized, 0, 1);
            // Round trip drifts, keep value unless slider moved
            return logValue.Equals(normalized) ? value : rangeConverter.ToRange(logValue);
        }

        public void DrawFloat(GUIContent label, float value, Action<float> onValueChanged, float clampMin = float.MinValue, float clampMax = float.MaxValue)
        {
            BeginDraw();
            EditorGUI.BeginChangeCheck();
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

            value = DrawSlider(value);

            value = SirenixEditorFields.FloatField(value, GUILayout.Width(50));
            value = Mathf.Clamp(value, Mathf.Max(clampMin, Min), Mathf.Min(clampMax, Max));

            SirenixEditorGUI.EndHorizontalPropertyLayout();
            if (EditorGUI.EndChangeCheck())
            {
                onValueChanged?.Invoke(value);
            }
        }

        public void DrawInt(GUIContent label, int value, Action<int> onValueChanged, int clampMin = int.MinValue, int clampMax = int.MaxValue)
        {
            BeginDraw();
            EditorGUI.BeginChangeCheck();
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

            var floatValue = DrawSlider(value);
            GUILayout.Space(2);

            floatValue = Mathf.Clamp(floatValue, clampMin, clampMax);
            value = Mathf.RoundToInt(floatValue);
            value = SirenixEditorFields.IntField(value, GUILayout.Width(50));
            value = Mathf.Clamp(value, Mathf.Max(clampMin, Mathf.RoundToInt(Min)), Mathf.Min(clampMax, Mathf.RoundToInt(Max)));

            SirenixEditorGUI.EndHorizontalPropertyLayout();
            if (EditorGUI.EndChangeCheck())
            {
                onValueChanged?.Invoke(value);
            }
        }
    }

    public readonly struct LogRangeConverter
    {
        private readonly float _a;
        private readonly float _b;
        private readonly float _c;
        private readonly float _min;
        private readonly float _max;
        private readonly bool _linear;
        public LogRangeConverter(float minValue, float centerValue, float maxValue)
        {
            _min = minValue;
            _max = maxValue;
            var denominator = minValue - 2 * centerValue + maxValue;
            // Center at midpoint or outside range, curve undefined
            _linear = Mathf.Abs(denominator) < 1e-6f || centerValue <= Mathf.Min(minValue, maxValue) || centerValue >= Mathf.Max(minValue, maxValue);
            if (_linear)
            {
                _a = _b = _c = 0f;
                return;
            }
            _a = (minValue * maxValue - centerValue * centerValue) / denominator;
            _b = (centerValue - minValue) * (centerValue - minValue) / denominator;
            _c = 2 * Mathf.Log((maxValue - centerValue) / (centerValue - minValue));
        }
        public float ToRange(float value)
        {
            if (_linear)
                return Mathf.Lerp(_min, _max, value);
            return _a + _b * Mathf.Exp(_c * value);
        }
        public float ToNormalized(float value)
        {
            if (_linear)
                return Mathf.InverseLerp(_min, _max, value);
            var normalized = Mathf.Log((value - _a) / _b) / _c;
            return float.IsNaN(normalized) ? (value <= _min ? 0f : 1f) : normalized;
        }
    }
}


