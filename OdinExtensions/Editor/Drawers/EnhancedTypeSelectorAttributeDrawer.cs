using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using ActionNamedValue = Sirenix.OdinInspector.Editor.ActionResolvers.NamedValue;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public class EnhancedTypeSelectorAttributeDrawer<T> : OdinAttributeDrawer<EnhancedTypeSelectorAttribute, T>
    {
        private ActionResolver _onTypeChanged;

        private ValueResolver<Type> _baseTypeResolver;
        private ValueResolver<IEnumerable<Type>> _customTypesResolver;

        private const string PREV_VALUE_ID = "prevValue";
        private const string NEW_VALUE_ID = "newValue";

        private bool _showInInlineEditors;

        protected override bool CanDrawAttributeValueProperty(InspectorProperty property)
        {
            return property.ChildResolver is not ICollectionResolver;
        }

        protected override void Initialize()
        {
            if (Attribute.OnTypeChanged != null)
            {
                _onTypeChanged = ActionResolver.Get(Property, Attribute.OnTypeChanged,
                    new ActionNamedValue(PREV_VALUE_ID, typeof(object)),
                    new ActionNamedValue(NEW_VALUE_ID, typeof(object)));
            }

            if (Attribute.BaseTypeGetter != null || Attribute.BaseType != null)
            {
                _baseTypeResolver = ValueResolver.Get(Property, Attribute.BaseTypeGetter, Attribute.BaseType);
            }

            if (Attribute.CustomTypesGetter != null)
            {
                _customTypesResolver = ValueResolver.Get<IEnumerable<Type>>(Property, Attribute.CustomTypesGetter);
            }

            _showInInlineEditors = Attribute.HasShowInInlineEditors ? Attribute.ShowInInlineEditors : !typeof(Object).IsAssignableFrom(ValueEntry.BaseValueType);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (!_showInInlineEditors && EnhancedInlineEditorAttributeDrawer.UniversalMaxCurrentInlineEditorDrawDepth > 0)
            {
                CallNextDrawer(label);
                return;
            }

            EditorGUILayout.BeginVertical();
            ActionResolver.DrawErrors(_onTypeChanged);
            ValueResolver.DrawErrors(_baseTypeResolver, _customTypesResolver);

            var selectedType = ValueEntry.SmartValue?.GetType();
            var dropdownButtonLabel = selectedType == null
                ? $"None ({GetSelectorNiceName(typeof(T), Attribute.UseGenericRegistryItems)})"
                : GetSelectorNiceName(selectedType, Attribute.UseGenericRegistryItems);

            GUIContent dropdownLabel;
            if (Attribute.HideDropdownLabel)
                dropdownLabel = null;
            else if (Property.IsTreeRoot)
                dropdownLabel = new GUIContent("Type");
            else
                dropdownLabel = label;

            var iconType = ValueEntry.SmartValue?.GetType() ?? typeof(T);
            SdfIconType valueIcon;
            if (typeof(Object).IsAssignableFrom(iconType))
                valueIcon = SdfIconType.None;
            else
            {
                var resolvedIcon = GetSelectorIcon(iconType, Attribute.UseGenericRegistryItems);
                valueIcon = resolvedIcon != SdfIconType.None ? resolvedIcon : SdfIconType.PuzzleFill;
            }

            var inlineFoldout = !typeof(Object).IsAssignableFrom(ValueEntry.BaseValueType);
            if (inlineFoldout)
            {
                bool expanded;
                if (dropdownLabel == null)
                {
                    var fieldRect = EditorGUILayout.GetControlRect(false);
                    DrawDropdown(fieldRect);
                    expanded = true;
                }
                else
                {
                    var rowRect = EditorGUILayout.GetControlRect(false);
                    var fieldRect = rowRect;
                    fieldRect.xMin = rowRect.x + EditorGUIUtility.labelWidth;
                    var foldoutRect = rowRect;
                    foldoutRect.xMax = fieldRect.xMin;

                    if (Attribute.HideFoldout)
                    {
                        EditorGUI.LabelField(foldoutRect, dropdownLabel);
                        expanded = true;
                    }
                    else
                    {
                        Property.State.Expanded = SirenixEditorGUI.Foldout(foldoutRect, Property.State.Expanded, dropdownLabel);
                        expanded = Property.State.Expanded;
                    }

                    DrawDropdown(fieldRect);
                }

                if (SirenixEditorGUI.BeginFadeGroup(this, expanded))
                {
                    if (Attribute.Space > 0)
                        GUILayout.Space(Attribute.Space);

                    GUIHelper.PushIndentLevel(EditorGUI.indentLevel + Attribute.ContentIndent);
                    foreach (var child in Property.Children)
                    {
                        child.Draw(child.Label);
                    }

                    GUIHelper.PopIndentLevel();
                }

                SirenixEditorGUI.EndFadeGroup();
            }
            else
            {
                SirenixEditorGUI.GetFeatureRichControlRect(dropdownLabel, out _, out _, out var fieldRect);
                DrawDropdown(fieldRect);

                if (Attribute.Space > 0)
                {
                    GUILayout.Space(Attribute.Space);
                }

                GUIHelper.PushIndentLevel(EditorGUI.indentLevel + Attribute.ContentIndent);
                CallNextDrawer(label);
                GUIHelper.PopIndentLevel();
            }

            EditorGUILayout.EndVertical();
            return;

            void DrawDropdown(Rect fieldRect)
            {
                TypeSelectorV2.DrawSelectorDropdown(fieldRect, new GUIContent(dropdownButtonLabel), rect =>
                {
                    IEnumerable<Type> types;
                    if (_customTypesResolver != null && !_customTypesResolver.HasError)
                    {
                        types = _customTypesResolver.GetValue();
                    }
                    else
                    {
                        var baseType = _baseTypeResolver?.GetValue() ?? ValueEntry.BaseValueType;
                        types = TypeCacheUtils.GetAssignableTypes(baseType);
                    }

                    var selector = new EnhancedTypeSelector(types, selectedType ?? typeof(T), Attribute.UseGenericRegistryItems);
                    selector.SetSelection(selectedType ?? typeof(T));
                    selector.DrawConfirmSelectionButton = true;
                    selector.SelectionConfirmed += selection =>
                    {
                        var newType = selection.FirstOrDefault();
                        Property.Tree.DelayActionUntilRepaint(() => ChangeType(newType));
                    };

                    selector.ShowInPopup(rect);
                    return selector;
                }, true, null, valueIcon);
            }
        }

        private void ChangeType(Type newType)
        {
            if (newType == null)
                return;
            var prevValue = ValueEntry.SmartValue;

            if (ValueEntry.SmartValue is Object unityObject)
            {
                // Changing script type for UnityEngine.Object is pretty hacky stuff so we have utility for that
                // We are not assigning it anywhere because this refreshes inspector anyway, assigning it may cause exception
                unityObject.ChangeType(newType, newValue =>
                {
                    if (_onTypeChanged != null && !_onTypeChanged.HasError)
                    {
                        _onTypeChanged.Context.NamedValues.Set(PREV_VALUE_ID, prevValue);
                        _onTypeChanged.Context.NamedValues.Set(NEW_VALUE_ID, newValue);
                        _onTypeChanged.DoAction();
                    }
                });
            }
            else
            {
                object newValue;
                if (ValueEntry.SmartValue == null)
                {
                    // Create new instance if original value is null
                    newValue = (T) Activator.CreateInstance(newType);
                }
                else
                {
                    // Unity JsonUtility is used because it uses the same serialization layout as assets
                    newValue = JsonUtility.FromJson(JsonUtility.ToJson(ValueEntry.SmartValue), newType);
                }

                ValueEntry.WeakSmartValue = newValue;

                if (_onTypeChanged != null && !_onTypeChanged.HasError)
                {
                    _onTypeChanged.Context.NamedValues.Set(PREV_VALUE_ID, prevValue);
                    _onTypeChanged.Context.NamedValues.Set(NEW_VALUE_ID, newValue);
                    _onTypeChanged.DoAction();
                }
            }
        }

        private static string GetSelectorNiceName(Type type, bool useGenericRegistryItems)
        {
            var userSettings = TypeRegistryUserConfig.Instance.TryGetSettings(type);
            if (userSettings != null && !string.IsNullOrEmpty(userSettings.Name))
                return userSettings.Name;

            var item = GetRegistryItem(type, useGenericRegistryItems);
            if (item != null && !string.IsNullOrEmpty(item.Name))
                return item.Name;

            return type.GetNiceName();
        }

        private static SdfIconType GetSelectorIcon(Type type, bool useGenericRegistryItems)
        {
            var userSettings = TypeRegistryUserConfig.Instance.TryGetSettings(type);
            if (userSettings != null && userSettings.Icon != SdfIconType.None)
                return userSettings.Icon;

            var item = GetRegistryItem(type, useGenericRegistryItems);
            return item?.Icon ?? SdfIconType.None;
        }

        private static TypeRegistryItemAttribute GetRegistryItem(Type type, bool useGenericRegistryItems)
        {
            if (type.IsConstructedGenericType && !useGenericRegistryItems)
                return null;
            return type.GetAttribute<TypeRegistryItemAttribute>(false);
        }

        // Odin's menu items take their name from the exact type when first drawn, so closed generics are renamed after that
        private class EnhancedTypeSelector : TypeSelectorV2
        {
            private readonly bool _useGenericRegistryItems;
            private readonly List<(OdinMenuItem Item, string Name, SdfIconType Icon)> _renamedItems = new();

            public EnhancedTypeSelector(IEnumerable<Type> types, Type selectedType, bool useGenericRegistryItems)
                : base(types, supportsMultiSelect: false, selectedType: selectedType, showNoneItem: false)
            {
                _useGenericRegistryItems = useGenericRegistryItems;
            }

            protected override void BuildSelectionTree(OdinMenuTree tree)
            {
                base.BuildSelectionTree(tree);
                _renamedItems.Clear();
                if (!_useGenericRegistryItems)
                    return;

                foreach (var item in tree.EnumerateTree())
                {
                    if (item.Value is not Type { IsConstructedGenericType: true } type)
                        continue;
                    if (GetRegistryItem(type, true) == null)
                        continue;

                    _renamedItems.Add((item, GetSelectorNiceName(type, true), GetSelectorIcon(type, true)));
                }
                ApplyRenamedItems();
            }

            protected override void DrawSelectionTree()
            {
                ApplyRenamedItems();
                base.DrawSelectionTree();
                ApplyRenamedItems();
            }

            private void ApplyRenamedItems()
            {
                foreach (var (item, name, icon) in _renamedItems)
                {
                    item.Name = name;
                    item.SearchString = name;
                    if (icon != SdfIconType.None)
                        item.SdfIcon = icon;
                }
            }
        }
    }
}
