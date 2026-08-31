using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public sealed class ColorTemperatureAttributeDrawer : OdinAttributeDrawer<ColorTemperatureAttribute, float>
    {
        private const float MIN_KELVIN = 1500;
        private const float NEUTRAL_KELVIN = 6500;
        private const float MAX_KELVIN = 20000;

        private const int GRADIENT_RESOLUTION = 256;

        private const string ICONS_PATH = "Packages/com.unity.render-pipelines.core/Editor/Lighting/Icons/LightUnitIcons";

        private static readonly LogRangeConverter KELVIN_RANGE = new(MIN_KELVIN, NEUTRAL_KELVIN, MAX_KELVIN);

        private static readonly TemperatureRange[] RANGES =
        {
            new("Blue Sky", "BlueSky", 10000, 20000, 15000),
            new("Shade (Clear Sky)", "Shade", 7000, 10000, 8000),
            new("Cloudy Skylight", "CloudySky", 6000, 7000, 6500),
            new("Direct Sunlight", "DirectSunlight", 4500, 6000, 5500),
            new("Fluorescent Light", "Fluorescent", 3500, 4500, 4000),
            new("Incandescent Light", "IntenseAreaLight", 2500, 3500, 3000),
            new("Candlelight", "Candlelight", 1500, 2500, 1900)
        };

        private static readonly Dictionary<string, Texture2D> ICONS = new();

        private static GUIStyle _sliderStyle;
        private static GUIStyle _thumbStyle;
        private static GUIStyle _iconButtonStyle;
        private static Texture2D _gradientTexture;

        private static GUIStyle SliderStyle => _sliderStyle ??= new GUIStyle("ColorPickerSliderBackground");
        private static GUIStyle ThumbStyle => _thumbStyle ??= new GUIStyle("ColorPickerHorizThumb");
        private static GUIStyle IconButtonStyle => _iconButtonStyle ??= new GUIStyle("IconButton");

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var value = Mathf.Clamp(ValueEntry.SmartValue, MIN_KELVIN, MAX_KELVIN);
            var range = GetRange(value);

            EditorGUI.BeginChangeCheck();
            SirenixEditorGUI.BeginVerticalPropertyLayout(label);

            var lineRect = EditorGUILayout.GetControlRect(false);
            var iconRect = lineRect.AlignRight(EditorGUIUtility.singleLineHeight);
            var sliderRect = lineRect.SetXMax(iconRect.xMin);

            GUI.DrawTexture(sliderRect, GetGradientTexture());

            EditorGUI.BeginChangeCheck();
            var sliderValue = GUI.HorizontalSlider(sliderRect, KELVIN_RANGE.ToNormalized(value), 0, 1, SliderStyle, ThumbStyle);
            if (EditorGUI.EndChangeCheck())
                value = Mathf.Round(KELVIN_RANGE.ToRange(sliderValue));

            DrawRangeIcon(iconRect, range, value);

            EditorGUILayout.BeginHorizontal();
            value = Mathf.Clamp(SirenixEditorFields.FloatField(value), MIN_KELVIN, MAX_KELVIN);
            GUILayout.Label("Kelvin");
            EditorGUILayout.EndHorizontal();

            SirenixEditorGUI.EndVerticalPropertyLayout();
            if (EditorGUI.EndChangeCheck())
                ValueEntry.SmartValue = value;
        }

        private static Texture2D GetGradientTexture()
        {
            if (_gradientTexture == null)
                _gradientTexture = CreateGradientTexture();
            return _gradientTexture;
        }

        private static TemperatureRange GetRange(float value)
        {
            return RANGES.TryGetFirst(r => r.Contains(value), out var rangeVal) ? rangeVal : RANGES[0];
        }

        private void DrawRangeIcon(Rect rect, TemperatureRange range, float value)
        {
            GUI.Box(rect, GUIContent.none, IconButtonStyle);

            var icon = GetIcon(range.IconName);
            if (icon != null)
            {
                var previousColor = GUI.color;
                GUI.color = Color.clear;
                EditorGUI.DrawTextureTransparent(rect, icon);
                GUI.color = previousColor;
            }
            else
            {
                EditorIcons.TriangleDown.Draw(rect);
            }

            var currentEvent = Event.current;
            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || !rect.Contains(currentEvent.mousePosition))
                return;

            ShowRangeMenu(rect, value);
            currentEvent.Use();
        }

        private void ShowRangeMenu(Rect rect, float value)
        {
            var menu = new GenericMenu();
            foreach (var range in RANGES)
            {
                var presetValue = range.PresetValue;
                menu.AddItem(new GUIContent(range.Label), range.Contains(value),
                    () => Property.Tree.DelayActionUntilRepaint(() => ValueEntry.SmartValue = presetValue));
            }
            menu.DropDown(new Rect(rect.position + rect.size, Vector2.zero));
        }
        
        private static Texture2D GetIcon(string iconName)
        {
            if (ICONS.TryGetValue(iconName, out var icon))
                return icon;

            var prefix = EditorGUIUtility.isProSkin ? "d_" : "";
            icon = EditorGUIUtility.Load($"{ICONS_PATH}/{prefix}{iconName}.png") as Texture2D;
            ICONS[iconName] = icon;
            return icon;
        }

        private static Texture2D CreateGradientTexture()
        {
            var texture = new Texture2D(GRADIENT_RESOLUTION, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[GRADIENT_RESOLUTION];
            for (var i = 0; i < GRADIENT_RESOLUTION; i++)
            {
                var kelvin = KELVIN_RANGE.ToRange(i / (GRADIENT_RESOLUTION - 1f));
                pixels[i] = Mathf.CorrelatedColorTemperatureToRGB(kelvin).gamma;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private readonly struct TemperatureRange
        {
            public readonly string Label;
            public readonly string IconName;
            public readonly float Min;
            public readonly float Max;
            public readonly float PresetValue;

            public TemperatureRange(string label, string iconName, float min, float max, float presetValue)
            {
                Label = label;
                IconName = iconName;
                Min = min;
                Max = max;
                PresetValue = presetValue;
            }

            public bool Contains(float value) => value >= Min && value <= Max;
        }
    }
}
