// Author: František Holubec
// Created: 14.10.2026

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Utils.Coloring
{
    public static class ColorPaletteGUI
    {
        private const float SWATCH_SIZE = 18f;
        private const float SPACING = 3f;
        private const float MARGIN = 30f;
        private const float STEP = SWATCH_SIZE + SPACING;
        private const int POPUP_COLUMNS = 10;
        private const int POPUP_EXTRA = 8;
        private const int POPUP_PADDING = 6;
        private const int RANDOM_RANGE_WHEN_EMPTY = 8;

        private static readonly Color SELECTED_COLOR = Color.white;
        private static readonly Color GENERATED_MARK_COLOR = new(0f, 0f, 0f, 0.5f);

        // Palette colors plus a few generated ones past the list. Returns the clicked index or -1
        public static int DrawSwatches(AColorPalette palette, int selected, int extra, float indent = 0f)
        {
            var colors = Enumerable.Range(0, palette.ColorCount + extra).Select(palette.GetColor).ToList();
            return DrawSwatches(colors, selected, palette.ColorCount, indent);
        }

        // Palette swatches in a dropdown, closes on pick
        public static void ShowSwatchesPopup(Rect rect, AColorPalette palette, int selected, Action<int> onPick)
        {
            PopupWindow.Show(rect, new SwatchesPopup(palette, selected, onPick));
        }

        // Swatches from generatedFrom on get a corner mark. Returns the clicked index or -1
        public static int DrawSwatches(IReadOnlyList<Color> colors, int selected = -1, int generatedFrom = int.MaxValue, float indent = 0f, float reservedWidth = 0f)
        {
            var count = colors.Count;
            var estimatedWidth = Mathf.Max(STEP, EditorGUIUtility.currentViewWidth - indent - reservedWidth - MARGIN);
            var columns = Mathf.Max(1, Mathf.FloorToInt((estimatedWidth + SPACING) / STEP));
            var rows = Mathf.CeilToInt(count / (float) columns);
            var area = GUILayoutUtility.GetRect(estimatedWidth, rows * STEP, GUILayout.ExpandWidth(true));

            var clicked = -1;
            var current = Event.current;
            for (var i = 0; i < count; i++)
            {
                var column = i % columns;
                var row = i / columns;
                var rect = new Rect(area.x + column * STEP, area.y + row * STEP, SWATCH_SIZE, SWATCH_SIZE);
                var color = colors[i];
                var generated = i >= generatedFrom;

                if (current.type == EventType.Repaint)
                {
                    if (i == selected)
                        EditorGUI.DrawRect(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), SELECTED_COLOR);
                    EditorGUI.DrawRect(rect, color);
                    if (generated)
                        EditorGUI.DrawRect(new Rect(rect.xMax - 6, rect.yMax - 6, 6, 6), GENERATED_MARK_COLOR);
                }

                GUI.Label(rect, GUIHelper.TempContent("", $"{i}: {PerceptualColorUtility.GetName(color)}{(generated ? " (generated)" : "")}"));

                if (current.type != EventType.MouseDown || current.button != 0 || !rect.Contains(current.mousePosition))
                    continue;

                clicked = i;
                GUI.changed = true;
                current.Use();
            }
            return clicked;
        }

        private class SwatchesPopup : PopupWindowContent
        {
            private readonly AColorPalette _palette;
            private int _selected;
            private readonly Action<int> _onPick;
            private readonly GUIStyle _padding = new() { padding = new RectOffset(POPUP_PADDING, POPUP_PADDING, POPUP_PADDING, POPUP_PADDING) };

            public SwatchesPopup(AColorPalette palette, int selected, Action<int> onPick)
            {
                _palette = palette;
                _selected = selected;
                _onPick = onPick;
            }

            private int Count => _palette.ColorCount + POPUP_EXTRA;

            public override Vector2 GetWindowSize()
            {
                var columns = Mathf.Min(Count, POPUP_COLUMNS);
                var rows = Mathf.CeilToInt(Count / (float) columns);
                return new Vector2(columns * STEP + MARGIN + POPUP_PADDING * 2, rows * STEP + POPUP_PADDING * 2);
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.BeginVertical(_padding);
                var colors = Enumerable.Range(0, Count).Select(_palette.GetColor).ToList();
                var clicked = DrawSwatches(colors, _selected, _palette.ColorCount, 0f, POPUP_PADDING * 2);
                EditorGUILayout.EndVertical();

                var randomRect = new Rect(rect.xMax - POPUP_PADDING - SWATCH_SIZE, rect.y + POPUP_PADDING, SWATCH_SIZE, SWATCH_SIZE);
                if (SirenixEditorGUI.IconButton(randomRect, FontAwesomeEditorIcons.GetIcon(FontAwesomeEditorIconType.DiceSolid), "Random color"))
                {
                    _selected = UnityEngine.Random.Range(0, _palette.ColorCount > 0 ? _palette.ColorCount : RANDOM_RANGE_WHEN_EMPTY);
                    _onPick(_selected);
                    editorWindow.Repaint();
                }

                if (clicked < 0)
                    return;

                _onPick(clicked);
                editorWindow.Close();
            }
        }
    }
}
#endif