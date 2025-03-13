using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public class IntMaskSelector : OdinSelector<uint>
    {
        private readonly float _maxLabelWidth;
        private uint _currentValue;
        private uint _currentMouseOverValue;
        private readonly int _everythingValue;
        private readonly Color _unnamedMaskBgColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.5f, 0f, 0.05f) : new Color(01f, 0.5f, 0f, 0.1f);

        private readonly List<ValueDropdownItem<uint>> _layerValues;

        public IntMaskSelector(uint value, IEnumerable<ValueDropdownItem<uint>> layers, bool useUnnamed)
        {
            _currentValue = value;
            _layerValues = FixLayerCollection(layers, useUnnamed);
            if (useUnnamed)
            {
                _everythingValue = ~0;
                FillUnnamedLayers(_layerValues);
            }
            else
            {
                _everythingValue = 0;
                foreach (var layerValue in _layerValues)
                {
                    _everythingValue |= 1 << (int) layerValue.Value;
                }
            }

            foreach (var item in _layerValues)
            {
                _maxLabelWidth = Mathf.Max(_maxLabelWidth, SirenixGUIStyles.Label.CalcSize(new GUIContent(item.Text)).x);
            }
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Selection.SupportsMultiSelect = true;
            tree.Config.DrawSearchToolbar = false;
            tree.Config.SelectMenuItemsOnMouseDown = true;
            tree.DefaultMenuStyle.Offset += 15;

            tree.Add("None", 0);
            tree.Add("Everything", _everythingValue);

            var unnamedStyle = tree.DefaultMenuStyle.Clone();
            unnamedStyle.DefaultLabelStyle = SirenixGUIStyles.LeftAlignedGreyLabel;
            for (var index = 0; index < _layerValues.Count; index++)
            {
                var layerValue = _layerValues[index];
                var isEmpty = string.IsNullOrWhiteSpace(layerValue.Text);
                var result = tree.AddObjectAtPath(isEmpty ? $"Unnamed ({index})" : layerValue.Text, 1 << (int) layerValue.Value);
                var menuItem = result.FirstOrDefault();
                if (isEmpty && menuItem != null)
                {
                    menuItem.Style = unnamedStyle;
                }
            }

            LinqExtensions.ForEach(tree.EnumerateTree(), x => x.OnDrawItem += DrawFlagItem);
            DrawConfirmSelectionButton = false;
        }

        [OnInspectorGUI, PropertyOrder(-1000)]
        private void SpaceToggleFlag()
        {
            if (SelectionTree != OdinMenuTree.ActiveMenuTree)
            {
                return;
            }

            if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown && SelectionTree != null)
            {
                foreach (var item in SelectionTree.Selection)
                {
                    ToggleFlag(item);
                }

                TriggerSelectionChanged();

                Event.current.Use();
            }
        }

        protected override float DefaultWindowWidth()
        {
            return Mathf.Clamp(_maxLabelWidth + 50, 160, 400);
        }

        private void DrawUnnamedFlagItem(OdinMenuItem menuItem)
        {
            EditorGUI.DrawRect(menuItem.Rect.AlignTop(menuItem.Rect.height - (EditorGUIUtility.isProSkin ? 1 : 0)), _unnamedMaskBgColor);
        }

        private void DrawFlagItem(OdinMenuItem menuItem)
        {
            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp) && menuItem.Rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    ToggleFlag(menuItem);

                    TriggerSelectionChanged();
                }

                Event.current.Use();
            }

            if (Event.current.type == EventType.Repaint)
            {
                var val = GetMenuItemValue(menuItem);
                var isPowerOfTwo = (val & (val - 1)) == 0;

                if (val != 0 && !isPowerOfTwo)
                {
                    var isMouseOver = menuItem.Rect.Contains(Event.current.mousePosition);
                    if (isMouseOver)
                    {
                        _currentMouseOverValue = val;
                    }
                    else if (val == _currentMouseOverValue)
                    {
                        _currentMouseOverValue = 0;
                    }
                }

                var chked = (val & _currentValue) == val && !(val == 0 && _currentValue != 0);

                var rect = menuItem.Rect.AlignLeft(30).AlignCenter(EditorIcons.TestPassed.width, EditorIcons.TestPassed.height);
                if (chked)
                {
                    if (!EditorGUIUtility.isProSkin)
                    {
                        var tmp = GUI.color;
                        GUI.color = new Color(1, 0.7f, 1, 1);
                        GUI.DrawTexture(rect, EditorIcons.TestPassed);
                        GUI.color = tmp;
                    }
                    else
                    {
                        GUI.DrawTexture(rect, EditorIcons.TestPassed);
                    }
                }
                else
                {
                    GUI.DrawTexture(rect, EditorIcons.TestNormal);
                }
            }
        }

        private void ToggleFlag(OdinMenuItem menuItem)
        {
            var val = GetMenuItemValue(menuItem);
            if ((val & _currentValue) == val)
            {
                _currentValue = val == 0 ? 0 : (_currentValue & ~val);
            }
            else
            {
                _currentValue |= val;
            }

            if (Event.current.clickCount >= 2)
            {
                Event.current.Use();
            }
        }

        public override IEnumerable<uint> GetCurrentSelection()
        {
            yield return _currentValue;
        }

        public override void SetSelection(uint selected)
        {
            _currentValue = selected;
        }

        private static uint GetMenuItemValue(OdinMenuItem item)
        {
            return (uint)(item.Value is int val ? val : 0);
        }

        public static void FillUnnamedLayersForButton(List<ValueDropdownItem<uint>> layers)
        {
            for (var i = 0; i < 32; i++)
            {
                if (i >= layers.Count || layers[i].Value != i)
                {
                    layers.Insert(i, new ValueDropdownItem<uint>($"({i})", (uint) i));
                }
                else if (string.IsNullOrWhiteSpace(layers[i].Text))
                {
                    layers[i] = new ValueDropdownItem<uint>($"({i})", (uint) i);
                }
            }
        }

        public static void FillUnnamedLayers(List<ValueDropdownItem<uint>> layers)
        {
            for (var i = 0; i < 32; i++)
            {
                if (i >= layers.Count || layers[i].Value != i)
                {
                    layers.Insert(i, new ValueDropdownItem<uint>("", (uint) i));
                }
            }
        }

        public static string GetButtonLabel(uint value, IReadOnlyCollection<ValueDropdownItem<uint>> labels, Rect labelRect)
        {
            if (value == 0) return "None";

            var mixedValues = labels.Where(label => (value & (1u << (int) label.Value)) != 0).ToList();
            if(mixedValues.Count == labels.Count)
                return "Everything";

            var mixedLabel = string.Join(", ", mixedValues.Select(m => m.Text));
            var mixedLabelWidth = SirenixGUIStyles.Label.CalcSize(new GUIContent(mixedLabel)).x;

            return labelRect.width - 18 < mixedLabelWidth ? "Mixed..." : mixedLabel;
        }

        public static List<ValueDropdownItem<uint>> FixLayerCollectionForButton(IEnumerable<ValueDropdownItem<uint>> layers, bool fillUnnamed = false)
        {
            var fixedLayers = layers.EmptyIfNull().OrderBy(l => l.Value).ToList();
            if (fillUnnamed)
                FillUnnamedLayersForButton(fixedLayers);
            else
                fixedLayers.RemoveAll(l => string.IsNullOrWhiteSpace(l.Text));
            return fixedLayers;
        }

        public static List<ValueDropdownItem<uint>> FixLayerCollection(IEnumerable<ValueDropdownItem<uint>> layers, bool fillUnnamed = false)
        {
            var fixedLayers = layers.EmptyIfNull().OrderBy(l => l.Value).ToList();
            if (fillUnnamed)
                FillUnnamedLayers(fixedLayers);
            else
                fixedLayers.RemoveAll(l => string.IsNullOrWhiteSpace(l.Text));
            return fixedLayers;
        }

        public static void DrawMaskField(GUIContent label, uint value, IEnumerable<ValueDropdownItem<uint>> layers, Action<uint> valueSetter, bool fillUnnamed = false)
        {
            SirenixEditorGUI.GetFeatureRichControlRect(label, out _, out _, out var rect);
            var btnLabel = GetButtonLabel(value, FixLayerCollectionForButton(layers, fillUnnamed), rect);
            DrawSelectorDropdown(rect, btnLabel, selectorRect =>
            {
                var selector = new IntMaskSelector(value, layers, fillUnnamed);
                selector.ShowInPopup(selectorRect);
                selector.SelectionChanged += result =>
                {
                    valueSetter?.Invoke(result.FirstOrDefault());
                };
                return selector;
            });
        }
    }
}
