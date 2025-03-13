using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;
using ValueResolversNamedValue = Sirenix.OdinInspector.Editor.ValueResolvers.NamedValue;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class EnhancedDictionaryAttributeDrawer<TDictionary, TKey, TValue> : OdinAttributeDrawer<EnhancedDictionaryAttribute, TDictionary>, IDisposable
        where TDictionary : IDictionary<TKey, TValue>
    {
        private static readonly bool KEY_IS_VALUE_TYPE = typeof(TKey).IsValueType;
        private static GUIStyle _addKeyPaddingStyle;
        private static GUIStyle _listItemStyle;
        private static GUIStyle _oneLineMargin;
        private static GUIStyle _headerMargin;
        
        private GeneralDrawerConfig _config;
        private StrongDictionaryPropertyResolver<TDictionary, TKey, TValue> _dictionaryResolver;
        private bool _disableAddKey;
        private GUIContent _keyLabel;
        private float _keyLabelWidth;
        private LocalPersistentContext<float> _keyWidthOffset;
        private TKey _newKey;
        private string _newKeyErrorMessage;
        private bool? _newKeyIsValid;
        private TValue _newValue;

        private GUIPagingHelper _paging = new GUIPagingHelper();
        private bool _showAddKeyGUI;
        private IPropertyValueEntry<TKey> _tempKeyEntry;
        private TempKeyValuePair<TKey, TValue> _tempKeyValue;
        private IPropertyValueEntry<TValue> _tempValueEntry;
        private GUIContent _valueLabel;
        
        private ActionResolver _onTitleBarGUI;
        private ValueResolver<TValue> _customValueDrawer;
        private ValueResolver<TKey> _customKeyDrawer;

        private static GUIStyle OneLineMargin => _oneLineMargin ??= new GUIStyle {margin = new RectOffset(8, 0, 0, 0)};
        private static GUIStyle HeaderMargin => _headerMargin ??= new GUIStyle {margin = new RectOffset(40, 0, 0, 0)};
        private static GUIStyle AddKeyPaddingStyle => _addKeyPaddingStyle ??= new GUIStyle("CN Box") 
        {
            overflow = new RectOffset(0, 0, 1, 0),
            fixedHeight = 0,
            stretchHeight = false,
            padding = new RectOffset(10, 10, 10, 10)
        };

        protected override void Initialize()
        {
            _listItemStyle = new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(7, 20, 3, 3)
            };
            var entry = ValueEntry;
            
            if (Attribute.OnTitleBarGUI != null) _onTitleBarGUI = ActionResolver.Get(Property, Attribute.OnTitleBarGUI);
            if (Attribute.CustomKeyDrawer != null)
                _customKeyDrawer = ValueResolver.Get<TKey>(Property, Attribute.CustomKeyDrawer,
                    new ValueResolversNamedValue("callNextDrawer", typeof(Action)),
                    new ValueResolversNamedValue("value", typeof(TKey)),
                    new ValueResolversNamedValue("dictValue", typeof(TValue)));
            if (Attribute.CustomValueDrawer != null)
                _customValueDrawer = ValueResolver.Get<TValue>(Property, Attribute.CustomValueDrawer,
                    new ValueResolversNamedValue("callNextDrawer", typeof(Action)),
                    new ValueResolversNamedValue("value", typeof(TValue)),
                    new ValueResolversNamedValue("dictKey", typeof(TKey)));

            
            _keyWidthOffset = this.GetPersistentValue("KeyColumnWidth", Attribute.KeyColumnWidth);
            _disableAddKey = entry.Property.Tree.PrefabModificationHandler.HasPrefabs && entry.SerializationBackend == SerializationBackend.Odin && !entry.Property.SupportsPrefabModifications;
            _keyLabel = new GUIContent(Attribute.KeyLabel);
            _valueLabel = new GUIContent(Attribute.ValueLabel);
            _keyLabelWidth = EditorStyles.label.CalcSize(_keyLabel).x + 20;
      
            if (Attribute.ExpandedHasValue) Property.State.Expanded = Attribute.Expanded;
            
            if (!_disableAddKey)
            {
                _tempKeyValue = new TempKeyValuePair<TKey, TValue>();
                var tree = PropertyTree.Create(_tempKeyValue);
                tree.UpdateTree();
                _tempKeyEntry = (IPropertyValueEntry<TKey>) tree.GetPropertyAtPath("Key").ValueEntry;
                _tempValueEntry = (IPropertyValueEntry<TValue>) tree.GetPropertyAtPath("Value").ValueEntry;
                tree.Dispose();
            }
        }
        
        public void Dispose()
        {
            _tempKeyEntry?.Dispose();
            _tempValueEntry?.Dispose();
        }
        
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var entry = ValueEntry;
            
            _dictionaryResolver = entry.Property.ChildResolver as StrongDictionaryPropertyResolver<TDictionary, TKey, TValue>;
            _config = GeneralDrawerConfig.Instance;
            _paging.NumberOfItemsPerPage = _config.NumberOfItemsPrPage;
            _listItemStyle.padding.right = !entry.IsEditable || Attribute.IsReadOnly ? 4 : 20;

            ActionResolver.DrawErrors(_onTitleBarGUI);
            ValueResolver.DrawErrors(_customValueDrawer, _customKeyDrawer);
            
            SirenixEditorGUI.BeginIndentedVertical(SirenixGUIStyles.PropertyPadding);
            {
                _paging.Update(entry.Property.Children.Count);
                DrawToolbar(entry, label);
                _paging.Update(entry.Property.Children.Count);

                if (!_disableAddKey && Attribute.IsReadOnly == false)
                {
                    DrawAddKey(entry);
                }

                GUIHelper.BeginLayoutMeasuring();
                if (SirenixEditorGUI.BeginFadeGroup(UniqueDrawerKey.Create(entry.Property, this), Property.State.Expanded, out var t))
                {
                    var rect = SirenixEditorGUI.BeginVerticalList(false);
                    if (Attribute.DisplayMode == DictionaryDisplayOptions.OneLine)
                    {
                        var maxWidth = rect.width - 90;
                        rect.xMin = _keyWidthOffset.Value + 22;
                        rect.xMax = rect.xMin + 10;

                        GUIHelper.PushGUIEnabled(true);
                        _keyWidthOffset.Value = _keyWidthOffset.Value + SirenixEditorGUI.SlideRect(rect).x;
                        GUIHelper.PopGUIEnabled();

                        if (Event.current.type == EventType.Repaint)
                        {
                            _keyWidthOffset.Value = Mathf.Clamp(_keyWidthOffset.Value, 30, maxWidth);
                        }

                        if (_paging.ElementCount != 0)
                        {
                            var headerRect = SirenixEditorGUI.BeginListItem(false);
                            {
                                GUILayout.Space(14);
                                if (Event.current.type == EventType.Repaint)
                                {
                                    GUI.Label(headerRect.SetWidth(_keyWidthOffset.Value), _keyLabel, SirenixGUIStyles.LabelCentered);
                                    GUI.Label(headerRect.AddXMin(_keyWidthOffset.Value), _valueLabel, SirenixGUIStyles.LabelCentered);
                                    SirenixEditorGUI.DrawSolidRect(headerRect.AlignBottom(1), SirenixGUIStyles.BorderColor);
                                }
                            }
                            SirenixEditorGUI.EndListItem();
                        }
                    }

                    GUIHelper.PushHierarchyMode(false);
                    DrawElements(entry, label);
                    GUIHelper.PopHierarchyMode();
                    SirenixEditorGUI.EndVerticalList();
                }

                SirenixEditorGUI.EndFadeGroup();

                // Draw borders
                var outerRect = GUIHelper.EndLayoutMeasuring();
                if (t > 0.01f && Event.current.type == EventType.Repaint)
                {
                    var col = SirenixGUIStyles.BorderColor;
                    outerRect.yMin -= 1;
                    SirenixEditorGUI.DrawBorders(outerRect, 1, col);
                    col.a *= t;
                    if (Attribute.DisplayMode == DictionaryDisplayOptions.OneLine)
                    {
                        // Draw Slide Rect Border
                        outerRect.width = 1;
                        outerRect.x += _keyWidthOffset.Value + 13;
                        SirenixEditorGUI.DrawSolidRect(outerRect, col);
                    }
                }
            }
            SirenixEditorGUI.EndIndentedVertical();
        }

        private void DrawAddKey(IPropertyValueEntry<TDictionary> entry)
        {
            if (entry.IsEditable == false || Attribute.IsReadOnly)
            {
                return;
            }

            if (SirenixEditorGUI.BeginFadeGroup(this, _showAddKeyGUI))
            {
                GUILayout.BeginVertical(AddKeyPaddingStyle);
                {
                    if (typeof(TKey) == typeof(string) && _newKey == null)
                    {
                        _newKey = (TKey) (object) "";
                        _newKeyIsValid = null;
                    }

                    _newKeyIsValid ??= CheckKeyIsValid(entry, _newKey, out _newKeyErrorMessage);

                    _tempKeyEntry.Property.Tree.BeginDraw(false);
                    
                    {
               
                        _tempKeyEntry.Property.Update();

                        EditorGUI.BeginChangeCheck();

                        _tempKeyEntry.Property.Draw(_keyLabel);

                        var changed1 = EditorGUI.EndChangeCheck();
                        var changed2 = _tempKeyEntry.ApplyChanges();

                        if (changed1 || changed2)
                        {
                            _newKey = _tempKeyValue.Key;
                            UnityEditorEventUtility.EditorApplication_delayCall += () => _newKeyIsValid = null;
                            GUIHelper.RequestRepaint();
                        }
                    }

                    // Value
                    {
                        //this.TempKeyValue.value = this.NewValue;
                        _tempValueEntry.Property.Update();
                        _tempValueEntry.Property.Draw(_valueLabel);
                        _tempValueEntry.ApplyChanges();
                        _newValue = _tempKeyValue.Value;
                    }

                    _tempKeyEntry.Property.Tree.InvokeDelayedActions();
                    var changed = _tempKeyEntry.Property.Tree.ApplyChanges();

                    if (changed)
                    {
                        _newKey = _tempKeyValue.Key;
                        UnityEditorEventUtility.EditorApplication_delayCall += () => _newKeyIsValid = null;
                        GUIHelper.RequestRepaint();
                    }

                    _tempKeyEntry.Property.Tree.EndDraw();

                    GUIHelper.PushGUIEnabled(GUI.enabled && _newKeyIsValid.Value);
                    if (GUILayout.Button(_newKeyIsValid.Value ? "Add" : _newKeyErrorMessage))
                    {
                        var keys = new object[entry.ValueCount];
                        var values = new object[entry.ValueCount];

                        for (var i = 0; i < keys.Length; i++)
                        {
                            keys[i] = SerializationUtility.CreateCopy(_newKey);
                        }

                        for (var i = 0; i < values.Length; i++)
                        {
                            values[i] = SerializationUtility.CreateCopy(_newValue);
                        }

                        _dictionaryResolver.QueueSet(keys, values);
                        UnityEditorEventUtility.EditorApplication_delayCall += () => _newKeyIsValid = null;
                        GUIHelper.RequestRepaint();

                        entry.Property.Tree.DelayActionUntilRepaint(() =>
                        {
                            _newValue = default(TValue);
                            _tempKeyValue.Value = default(TValue);
                            _tempValueEntry.Update();
                        });
                    }

                    GUIHelper.PopGUIEnabled();
                }
                GUILayout.EndVertical();
            }

            SirenixEditorGUI.EndFadeGroup();
        }

        private void DrawToolbar(IPropertyValueEntry<TDictionary> entry, GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                if (entry.ListLengthChangedFromPrefab) GUIHelper.PushIsBoldLabel(true);

                if (_paging.ElementCount == 0)
                {
                    if (label != null)
                    {
                        GUILayout.Label(label, GUILayoutOptions.ExpandWidth(false));
                    }
                }
                else
                {
                    var newState = label != null
                        ? SirenixEditorGUI.Foldout(Property.State.Expanded, label)
                        : SirenixEditorGUI.Foldout(Property.State.Expanded, "");
                    if (!newState && Property.State.Expanded)
                    {
                        _showAddKeyGUI = false;
                    }

                    Property.State.Expanded = newState;
                }

                if (entry.ListLengthChangedFromPrefab) GUIHelper.PopIsBoldLabel();

                GUILayout.FlexibleSpace();

                // Item Count
                if (_config.ShowItemCount)
                {
                    if (entry.ValueState == PropertyValueState.CollectionLengthConflict)
                    {
                        var min = entry.Values.Min(x => x.Count);
                        var max = entry.Values.Max(x => x.Count);
                        GUILayout.Label(min + " / " + max + " items", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        GUILayout.Label(_paging.ElementCount == 0 ? "Empty" : _paging.ElementCount + " items", EditorStyles.centeredGreyMiniLabel);
                    }
                }

                var hidePaging =
                    _config.HidePagingWhileCollapsed && Property.State.Expanded == false ||
                    _config.HidePagingWhileOnlyOnePage && _paging.PageCount == 1;

                if (!hidePaging)
                {
                    var wasEnabled = GUI.enabled;
                    var pagingIsRelevant = _paging.IsEnabled && _paging.PageCount != 1;

                    GUI.enabled = wasEnabled && pagingIsRelevant && !_paging.IsOnFirstPage;
                    if (SirenixEditorGUI.ToolbarButton(EditorIcons.ArrowLeft, true))
                    {
                        if (Event.current.button == 0)
                        {
                            _paging.CurrentPage--;
                        }
                        else
                        {
                            _paging.CurrentPage = 0;
                        }
                    }

                    GUI.enabled = wasEnabled && pagingIsRelevant;
                    var width = GUILayoutOptions.Width(10 + _paging.PageCount.ToString().Length * 10);
                    _paging.CurrentPage = EditorGUILayout.IntField(_paging.CurrentPage + 1, width) - 1;
                    GUILayout.Label(GUIHelper.TempContent("/ " + _paging.PageCount));

                    GUI.enabled = wasEnabled && pagingIsRelevant && !_paging.IsOnLastPage;
                    if (SirenixEditorGUI.ToolbarButton(EditorIcons.ArrowRight, true))
                    {
                        if (Event.current.button == 0)
                        {
                            _paging.CurrentPage++;
                        }
                        else
                        {
                            _paging.CurrentPage = _paging.PageCount - 1;
                        }
                    }

                    GUI.enabled = wasEnabled && _paging.PageCount != 1;
                    if (_config.ShowExpandButton)
                    {
                        if (SirenixEditorGUI.ToolbarButton(_paging.IsEnabled ? EditorIcons.ArrowDown : EditorIcons.ArrowUp, true))
                        {
                            _paging.IsEnabled = !_paging.IsEnabled;
                        }
                    }

                    GUI.enabled = wasEnabled;
                }

                if (!_disableAddKey && Attribute.IsReadOnly != true)
                {
                    if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus))
                    {
                        _showAddKeyGUI = !_showAddKeyGUI;

                        if (_showAddKeyGUI)
                        {
                            Property.State.Expanded = true;
                        }
                    }
                }
            }
            
            if (Attribute.OnTitleBarGUI != null && !_onTitleBarGUI.HasError) 
                _onTitleBarGUI.DoAction();
            
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private void DrawElements(IPropertyValueEntry<TDictionary> entry, GUIContent label)
        {
            for (var i = _paging.StartIndex; i < _paging.EndIndex; i++)
            {
                var keyValuePairProperty = entry.Property.Children[i];
                var keyValuePairValue = ((IPropertyValueEntry<EditableKeyValuePair<TKey, TValue>>) keyValuePairProperty.ValueEntry).SmartValue;

                var rect = SirenixEditorGUI.BeginListItem(false, _listItemStyle);
                {
                    if (Attribute.DisplayMode != DictionaryDisplayOptions.OneLine)
                    {
                        bool defaultExpanded;
                        switch (Attribute.DisplayMode)
                        {
                            case DictionaryDisplayOptions.CollapsedFoldout:
                                defaultExpanded = false;
                                break;

                            case DictionaryDisplayOptions.ExpandedFoldout:
                                defaultExpanded = true;
                                break;

                            default:
                                defaultExpanded = SirenixEditorGUI.ExpandFoldoutByDefault;
                                break;
                        }

                        var isExpanded = keyValuePairProperty.Context.GetPersistent(this, "Expanded", defaultExpanded);

                        SirenixEditorGUI.BeginBox();
                        SirenixEditorGUI.BeginToolbarBoxHeader();
                        {
                            if (keyValuePairValue.IsInvalidKey)
                            {
                                GUIHelper.PushColor(Color.red);
                            }

                            var btnRect = GUIHelper.GetCurrentLayoutRect().AlignLeft(HeaderMargin.margin.left);
                            btnRect.y += 1;
                            GUILayout.BeginVertical(HeaderMargin);
                            GUIHelper.PushIsDrawingDictionaryKey(true);

                            GUIHelper.PushLabelWidth(_keyLabelWidth);

                            var keyProperty = keyValuePairProperty.Children[0];
                            var valueProperty = keyValuePairProperty.Children[1];
                            var newKeyLabel = GUIHelper.TempContent(" ");
                            DrawKeyProperty(keyProperty, newKeyLabel, valueProperty);

                            GUIHelper.PopLabelWidth();

                            GUIHelper.PopIsDrawingDictionaryKey();
                            GUILayout.EndVertical();
                            if (keyValuePairValue.IsInvalidKey)
                            {
                                GUIHelper.PopColor();
                            }

                            isExpanded.Value = SirenixEditorGUI.Foldout(btnRect, isExpanded.Value, this._keyLabel);
                        }
                        SirenixEditorGUI.EndToolbarBoxHeader();

                        if (SirenixEditorGUI.BeginFadeGroup(isExpanded, isExpanded.Value))
                        {
                            if (_customValueDrawer != null && !_customValueDrawer.HasError)
                            {
                                _customValueDrawer.Context.NamedValues.Set("dictKey", keyValuePairValue.Key);
                                _customValueDrawer.Context.NamedValues.Set("value", keyValuePairValue.Value);
                                _customValueDrawer.Context.NamedValues.Set("callNextDrawer", new Action(() =>
                                {
                                    keyValuePairProperty.Children[1].Draw(null);
                                }));
                        
                                keyValuePairProperty.Children[1].ValueEntry.WeakSmartValue = _customValueDrawer.GetValue();
                            }
                            else
                            {
                                keyValuePairProperty.Children[1].Draw(null);
                            }
                        }
                        
                        SirenixEditorGUI.EndFadeGroup();
                        
                        if (SirenixEditorGUI.BeginFadeGroup(isExpanded, isExpanded.Value))
                        {
                            keyValuePairProperty.Children[1].Draw(null);
                        }

                        SirenixEditorGUI.EndFadeGroup();

                        SirenixEditorGUI.EndToolbarBox();
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.BeginVertical(GUILayoutOptions.Width(_keyWidthOffset.Value));
                        {
                            var keyProperty = keyValuePairProperty.Children[0];
                            var valueProperty = keyValuePairProperty.Children[1];

                            if (keyValuePairValue.IsInvalidKey)
                            {
                                GUIHelper.PushColor(Color.red);
                            }

                            if (Attribute.IsReadOnly) GUIHelper.PushGUIEnabled(false);

                            GUIHelper.PushIsDrawingDictionaryKey(true);
                            GUIHelper.PushLabelWidth(10);

                            DrawKeyProperty(keyProperty, null, valueProperty);

                            GUIHelper.PopLabelWidth();
                            GUIHelper.PopIsDrawingDictionaryKey();

                            if (Attribute.IsReadOnly) GUIHelper.PopGUIEnabled();

                            if (keyValuePairValue.IsInvalidKey)
                            {
                                GUIHelper.PopColor();
                            }
                        }
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical(OneLineMargin);
                        {
                            GUIHelper.PushHierarchyMode(false);
                            var valueEntry = keyValuePairProperty.Children[1];
                            var tmp = GUIHelper.ActualLabelWidth;
                            GUIHelper.BetterLabelWidth = 150;
                            if (Attribute.CustomValueDrawer != null && !_customValueDrawer.HasError)
                            {
                                _customValueDrawer.Context.NamedValues.Set("dictKey", keyValuePairValue.Key);
                                _customValueDrawer.Context.NamedValues.Set("value", keyValuePairValue.Value);
                                _customValueDrawer.Context.NamedValues.Set("callNextDrawer", new Action(() =>
                                {
                                    valueEntry.Draw(null);
                                }));
                        
                                valueEntry.ValueEntry.WeakSmartValue = _customValueDrawer.GetValue();
                            }
                            else
                            {
                                valueEntry.Draw(null);
                            }
                            GUIHelper.BetterLabelWidth = tmp;
                            GUIHelper.PopHierarchyMode();
                        }
                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                    }

                    if (entry.IsEditable && !Attribute.IsReadOnly && SirenixEditorGUI.IconButton(new Rect(rect.xMax - 24 + 5, rect.y + 4 + ((int) rect.height - 23) / 2, 14, 14), EditorIcons.X))
                    {
                        _dictionaryResolver.QueueRemoveKey(Enumerable.Range(0, entry.ValueCount).Select(n => _dictionaryResolver.GetKey(n, i)).ToArray());
                        UnityEditorEventUtility.EditorApplication_delayCall += () => _newKeyIsValid = null;
                        GUIHelper.RequestRepaint();
                    }
                }
                SirenixEditorGUI.EndListItem();
            }

            if (_paging.IsOnLastPage && entry.ValueState == PropertyValueState.CollectionLengthConflict)
            {
                SirenixEditorGUI.BeginListItem(false);
                GUILayout.Label(GUIHelper.TempContent("------"), EditorStyles.centeredGreyMiniLabel);
                SirenixEditorGUI.EndListItem();
            }
        }

        private void DrawKeyProperty(InspectorProperty keyProperty, GUIContent newKeyLabel, InspectorProperty valueProperty)
        {
            EditorGUI.BeginChangeCheck();
            if (Attribute.CustomKeyDrawer != null && !_customKeyDrawer.HasError)
            {
                _customKeyDrawer.Context.NamedValues.Set("value", (TKey) keyProperty.ValueEntry.WeakSmartValue);
                _customKeyDrawer.Context.NamedValues.Set("dictValue", (TValue) valueProperty.ValueEntry.WeakSmartValue);
                _customKeyDrawer.Context.NamedValues.Set("callNextDrawer", new Action(() =>
                {
                    keyProperty.Draw(newKeyLabel);
                }));
                keyProperty.ValueEntry.WeakSmartValue = _customKeyDrawer.GetValue();
            }
            else
            {
                keyProperty.Draw(newKeyLabel);
            }
            var guiChanged = EditorGUI.EndChangeCheck();
            var valuesAreDirty = ValuesAreDirty(keyProperty);
            if (!guiChanged && valuesAreDirty)
            {
                _dictionaryResolver.ValueApplyIsTemporary = true;
                ApplyChangesToProperty(keyProperty);
                _dictionaryResolver.ValueApplyIsTemporary = false;
            }
            else if (guiChanged && !valuesAreDirty)
            {
                MarkPropertyDirty(keyProperty);
            }
        }

        private static void MarkPropertyDirty(InspectorProperty keyProperty)
        {
            keyProperty.ValueEntry.WeakValues.ForceMarkDirty();

            if (KEY_IS_VALUE_TYPE)
            {
                foreach (var child in keyProperty.Children)
                {
                    MarkPropertyDirty(child);
                }
            }
        }

        private static void ApplyChangesToProperty(InspectorProperty keyProperty)
        {
            if (keyProperty.ValueEntry != null && keyProperty.ValueEntry.WeakValues.AreDirty) keyProperty.ValueEntry.ApplyChanges();

            if (KEY_IS_VALUE_TYPE)
            {
                foreach (var child in keyProperty.Children)
                {
                    ApplyChangesToProperty(child);
                }
            }
        }

        private static bool ValuesAreDirty(InspectorProperty keyProperty)
        {
            if (keyProperty.ValueEntry != null && keyProperty.ValueEntry.WeakValues.AreDirty)
            {
                return true;
            }

            if (KEY_IS_VALUE_TYPE)
            {
                foreach (var child in keyProperty.Children)
                {
                    if (ValuesAreDirty(child)) return true;
                }
            }

            return false;
        }

        private static bool CheckKeyIsValid(IPropertyValueEntry<TDictionary> entry, TKey key, out string errorMessage)
        {
            if (!KEY_IS_VALUE_TYPE && ReferenceEquals(key, null))
            {
                errorMessage = "Key cannot be null.";
                return false;
            }

            var keyStr = DictionaryKeyUtility.GetDictionaryKeyString(key);

            if (entry.Property.Children[keyStr] == null)
            {
                errorMessage = "";
                return true;
            }

            errorMessage = "An item with the same key already exists.";
            return false;
        }
    }
}
