// Author: František Holubec
// Created: 16.10.2026

using System;
using System.Collections.Generic;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public class EditorIconGrid
    {
        private const float PADDING = 10;
        private static readonly Color SELECTED_COLOR = new(0f, 0.5f, 1f, 0.5f);
        private static readonly Color HOVER_COLOR = new(1f, 1f, 1f, 0.1f);

        private float _scrollPos;
        private float _scrollMax;

        public void ResetScroll() => _scrollPos = 0;

        public int Draw<T>(IReadOnlyList<T> items, Func<T, Texture> getTexture, Func<T, string> getName, float size, int selectedIndex = -1)
        {
            var clicked = -1;
            _scrollPos = EditorGUILayout.BeginScrollView(new Vector2(0, _scrollPos)).y;
            {
                var area = GUILayoutUtility.GetRect(0, _scrollMax, GUIStyle.none, GUILayoutOptions.ExpandHeight());
                var evt = Event.current;
                if (evt.type is EventType.Repaint or EventType.MouseDown && area.width > 1)
                {
                    var cellSize = size + PADDING;
                    var columns = Mathf.Max(1, (int) (area.width / cellSize));
                    cellSize += area.width % cellSize / columns;
                    area.width += 1;

                    var viewHeight = Screen.height / EditorGUIUtility.pixelsPerPoint;
                    var mousePos = evt.mousePosition;
                    var mouseOver = -1;
                    var yMax = 0f;

                    for (var i = 0; i < items.Count; i++)
                    {
                        var cell = area.SplitGrid(cellSize, cellSize, i);
                        yMax = Math.Max(yMax, cell.yMax);
                        if (cell.yMax < _scrollPos || cell.yMin > _scrollPos + viewHeight) continue;

                        var hover = cell.Contains(mousePos);
                        if (hover) mouseOver = i;
                        if (evt.type != EventType.Repaint) continue;

                        if (i == selectedIndex) SirenixEditorGUI.DrawSolidRect(cell, SELECTED_COLOR);
                        else if (hover) SirenixEditorGUI.DrawSolidRect(cell, HOVER_COLOR);

                        var texture = getTexture(items[i]);
                        if (texture != null)
                            GUI.DrawTexture(cell.AlignCenter(size, size).Padding(5), texture, ScaleMode.ScaleToFit);
                    }

                    if (evt.type == EventType.Repaint)
                    {
                        _scrollMax = yMax;
                        if (mouseOver >= 0)
                            DrawTooltip(getName(items[mouseOver]), mousePos, area);
                    }
                    else if (mouseOver >= 0 && evt.button == 0)
                    {
                        clicked = mouseOver;
                        evt.Use();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            return clicked;
        }

        private static void DrawTooltip(string label, Vector2 mousePos, Rect area)
        {
            var style = new GUIStyle(SirenixGUIStyles.WhiteLabel) {fontStyle = FontStyle.Bold};
            var size = style.CalcSize(new GUIContent(label));
            var rect = new Rect(mousePos + new Vector2(30, 5), size);

            var push = rect.xMax - area.xMax + 20;
            if (push > 0) rect.x -= push;

            SirenixEditorGUI.DrawSolidRect(rect.Expand(10, 5), Color.black);
            GUI.Label(rect, label, style);
        }
    }
}
