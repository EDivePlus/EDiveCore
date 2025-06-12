// Author: František Holubec
// Created: 12.06.2025

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Utils.FontSymbols
{
    public class FontSymbolSelectionWindow : OdinEditorWindow
    {
        private class CodepointData
        {
            public readonly Codepoint Codepoint;
            public readonly int Index;

            public readonly GUIContent LabelContent;
            public readonly GUIContent ImageContent;

            public CodepointData(Codepoint codepoint, int index)
            {
                Codepoint = codepoint;
                Index = index;

                LabelContent = new GUIContent(Codepoint.Name, Codepoint.HexCode);
                ImageContent = new GUIContent(Codepoint.Char.ToString(), $"{Codepoint.Name} ({Codepoint.HexCode})");
            }
        }

        [HorizontalGroup("Search")]
        [OnValueChanged(nameof(RunFilter))]
        [SerializeField]
        private string _SearchFilter;

        [SerializeField]
        [PropertyRange(20, 80)]
        private int _Size = 40;

        private GUIStyle _imageStyle;

        private FontSymbolsDefinition _definition;
        private List<CodepointData> _filteredCollection;
        private CodepointData _selected;
        private Action<Codepoint> _onSelectionChanged;
        private Vector2 _scrollPos = Vector2.zero;

        public static void Open(FontSymbolsDefinition definition, char preSelected = char.MinValue, Action<Codepoint> onSelectionChanged = null)
        {
            var window = GetWindow<FontSymbolSelectionWindow>(true);
            window.wantsMouseMove = true;
            window.Prepare(definition, preSelected, onSelectionChanged);
            window.ShowAuxWindow();
        }

        private void Prepare(FontSymbolsDefinition definition, char preSelected,  Action<Codepoint> onSelectionChanged)
        {
            _definition = definition;
            if (!_definition)
                return;

            _onSelectionChanged = onSelectionChanged;
            _filteredCollection = _definition.Codepoints.Entries.Select((c, i) => new CodepointData(c, i)).ToList();
            _selected = _filteredCollection.FirstOrDefault(data => data.Codepoint.Char == preSelected);

            _imageStyle = new GUIStyle(SirenixGUIStyles.WhiteLabelCentered)
            {
                font = _definition.Font,
                fontSize = _Size,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        [OnInspectorGUI]
        private void DrawTable()
        {
            if (!_definition)
            {
                EditorGUILayout.HelpBox("Invalid definition", MessageType.Error);
                return;
            }

            if (!_definition.Codepoints || _definition.Codepoints.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox("Invalid codepoints", MessageType.Error);
                return;
            }

            _imageStyle.fontSize = _Size;
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            var viewWidth = EditorGUIUtility.currentViewWidth - 20;

            var charSize = _imageStyle.CalcSize("W");
            var cellSize = charSize + Vector2.one * 10;

            var columns = Mathf.Max(1, Mathf.FloorToInt(viewWidth / cellSize.x));
            var contentWidth = columns * cellSize.x;
            var rows = Mathf.CeilToInt((float) _filteredCollection.Count / columns);
            var totalHeight = rows * cellSize.y;

            var area = GUILayoutUtility.GetRect(contentWidth, totalHeight);

            for (var i = 0; i < _filteredCollection.Count; i++)
            {
                var cell = area.SplitGrid(cellSize.x, cellSize.y, i);
                if (!IsVisible(cell, _scrollPos, area.height)) continue;

                var icon = _filteredCollection[i];
                var cellAligned = cell.AlignCenter(charSize.x, charSize.y);
                var hover = cellAligned.Contains(Event.current.mousePosition);
                var active = icon == _selected;

                if (Event.current.type == EventType.Repaint)
                {
                    if (active) EditorGUI.DrawRect(cell, new Color(0f, 0.5f, 1f, 0.5f));
                    else if (hover) EditorGUI.DrawRect(cell, new Color(1f, 1f, 1f, 0.1f));

                    _imageStyle.Draw(cellAligned, icon.ImageContent, 0);
                }

                if (hover && Event.current.type == EventType.MouseDown)
                {
                    Select(icon);
                    if (Event.current.clickCount == 2)
                    {
                        EditorApplication.delayCall += base.Close;
                        GUIUtility.ExitGUI();
                    }
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static bool IsVisible(Rect cell, Vector2 scrollPos, float viewHeight)
        {
            return cell.yMax > scrollPos.y && cell.yMin < scrollPos.y + viewHeight;
        }

        private void RunFilter()
        {
            _filteredCollection = _definition.Codepoints.Entries.Select((c, i) => new CodepointData(c, i)).ToList();

            if (!string.IsNullOrEmpty(_SearchFilter))
            {
                _filteredCollection = _filteredCollection.Where(d => FuzzySearch.Contains(_SearchFilter, d.Codepoint.Name)).ToList();
            }

            _filteredCollection.Sort((data, other) => string.Compare(data.Codepoint.Name, other.Codepoint.Name, StringComparison.Ordinal));
            _scrollPos.y = 0f;
        }

        private void Select(CodepointData data)
        {
            if (_onSelectionChanged != null)
            {
                _selected = data;
                _onSelectionChanged?.Invoke(data.Codepoint);
            }
        }
    }
}
#endif
