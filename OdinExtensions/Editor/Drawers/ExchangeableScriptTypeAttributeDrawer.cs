using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEditor;
using UnityEngine;
using ActionNamedValue = Sirenix.OdinInspector.Editor.ActionResolvers.NamedValue;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public class ExchangeableScriptTypeAttributeDrawer<T> : OdinAttributeDrawer<ExchangeableScriptTypeAttribute, T>
    {
        private ActionResolver _onScriptChanged;

        private ValueResolver<Type> _baseTypeResolver;
        private ValueResolver<IEnumerable<ValueDropdownItem<Type>>> _customTypesResolver;

        private const string PREV_VALUE_ID = "prevValue";
        private const string NEW_VALUE_ID = "newValue";

        private bool _showInInlineEditors;

        protected override void Initialize()
        {
            if (Attribute.OnScriptChanged != null)
            {
                _onScriptChanged = ActionResolver.Get(Property, Attribute.OnScriptChanged,
                    new ActionNamedValue(PREV_VALUE_ID, typeof(object)), 
                    new ActionNamedValue(NEW_VALUE_ID, typeof(object)));
            }
            
            if (Attribute.BaseTypeGetter != null || Attribute.BaseType != null)
            {
                _baseTypeResolver = ValueResolver.Get(Property, Attribute.BaseTypeGetter, Attribute.BaseType);
            }

            if (Attribute.CustomTypesGetter != null)
            {
                _customTypesResolver = ValueResolver.Get<IEnumerable<ValueDropdownItem<Type>>>(Property, Attribute.CustomTypesGetter);
            }

            _showInInlineEditors = Attribute.HasShowInInlineEditors ? Attribute.ShowInInlineEditors : !typeof(Object).IsAssignableFrom(ValueEntry.BaseValueType);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            // Draw only if property is tree root in case of value is type of UnityEngine.Object
            if (!_showInInlineEditors && EnhancedInlineEditorAttributeDrawer.UniversalMaxCurrentInlineEditorDrawDepth > 0)
            {
                CallNextDrawer(label);
                return;
            }
            
            // Draw only if property is tree root in case of value is type of UnityEngine.Object
            if (typeof(Object).IsAssignableFrom(ValueEntry.BaseValueType) && !Property.IsTreeRoot)
            {
                CallNextDrawer(label);
                return;
            }
 
            EditorGUILayout.BeginVertical();
            ActionResolver.DrawErrors(_onScriptChanged);
            ValueResolver.DrawErrors(_baseTypeResolver, _customTypesResolver);

            var typeName = typeof(T).GetFriendlyName();
            var dropdownButtonLabel = ValueEntry.SmartValue == null ? $"Null - {typeName}" : ObjectNames.NicifyVariableName(typeName);
            var dropdownLabel = Attribute.HideDropdownLabel ? null : new GUIContent("Type");
            GenericSelector<Type>.DrawSelectorDropdown(dropdownLabel, dropdownButtonLabel, rect =>
            {
                var baseType = _baseTypeResolver?.GetValue() ?? ValueEntry.BaseValueType;
                GenericSelector<Type> selector;
                if (_customTypesResolver != null && !_customTypesResolver.HasError)
                {
                    var values = _customTypesResolver.GetValue()
                        .Select(t => new GenericSelectorItem<Type>(t.Text, t.Value));
                    selector = new GenericSelector<Type>(null, false, values);
                }
                else
                {
                    var types = TypeCacheUtils.GetAssignableTypes(baseType);
                    selector = new GenericSelector<Type>(null, false, x => $"{x.Name} ({x.Namespace})", types);
                }
                selector.SetSelection(typeof(T));
                selector.DrawConfirmSelectionButton = true;
                selector.SelectionTree.DefaultMenuStyle.Height = 22;
                selector.SelectionTree.Config.DrawSearchToolbar = true;
                selector.SelectionTree.Config.AutoFocusSearchBar = true;
                selector.SelectionTree.EnumerateTree().AddThumbnailIcons(true);
                selector.SelectionConfirmed += selection =>
                {
                    var newType = selection.FirstOrDefault();
                    Property.Tree.DelayActionUntilRepaint(() => ChangeScriptType(newType));
                };

                selector.ShowInPopup(rect);
                return selector;
            });
            if (Attribute.Space > 0)
            {
                GUILayout.Space(Attribute.Space);
            }
            CallNextDrawer(label);
            EditorGUILayout.EndVertical();
        }

        private void ChangeScriptType(Type newType)
        {
            var prevValue = ValueEntry.SmartValue;
     
            if (ValueEntry.SmartValue is Object unityObject)
            {
                // Changing script type for UnityEngine.Object is pretty hacky stuff so we have utility for that
                // We are not assigning it anywhere because this refreshes inspector anyway, assigning it may cause exception
                unityObject.ChangeScriptType(newType, newValue =>
                {
                    if (_onScriptChanged != null && !_onScriptChanged.HasError)
                    {
                        _onScriptChanged.Context.NamedValues.Set(PREV_VALUE_ID, prevValue);
                        _onScriptChanged.Context.NamedValues.Set(NEW_VALUE_ID, newValue);
                        _onScriptChanged.DoAction();
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
                
                if (_onScriptChanged != null && !_onScriptChanged.HasError)
                {
                    _onScriptChanged.Context.NamedValues.Set(PREV_VALUE_ID, prevValue);
                    _onScriptChanged.Context.NamedValues.Set(NEW_VALUE_ID, newValue);
                    _onScriptChanged.DoAction();
                }
            }
        }
    }
}
