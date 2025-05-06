using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public class EnhancedObjectDrawerAttributeDrawer<T> : OdinAttributeDrawer<EnhancedObjectDrawerAttribute, T> where T : Object
    {
        private ValueResolver<bool> _elementConditionResolver;
        private ValueResolver<Type> _preferredTypeResolver;

        protected override void Initialize()
        {
            if (!string.IsNullOrEmpty(Attribute.ElementCondition))
                _elementConditionResolver = ValueResolver.Get<bool>(Property, Attribute.ElementCondition, new NamedValue("value", typeof(Object)));

            if (!string.IsNullOrEmpty(Attribute.PreferredTypeGetter))
                _preferredTypeResolver = ValueResolver.Get<Type>(Property, Attribute.PreferredTypeGetter);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_elementConditionResolver, _preferredTypeResolver);
            EditorGUI.BeginChangeCheck();
            var fieldRect = EditorGUILayout.BeginHorizontal();
            Object prevValue = ValueEntry.SmartValue != null ? ValueEntry.SmartValue : null;
            var fieldType = ValueEntry.BaseValueType;
            var newValue = SirenixEditorFields.UnityObjectField(label, prevValue, fieldType, true);
            var targetGameObject = newValue is Component cmp ? cmp.gameObject : newValue as GameObject;

            var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
            GUIHelper.PushGUIEnabled(prevValue != null);
            if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.SquareCaretDownSolid, "Select object"))
            {
                ShowSelector(fieldRect, targetGameObject);
            }
            GUIHelper.PopGUIEnabled();

            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                var allObjects = GetAllObjects(targetGameObject).ToList();

                if (TryGetPreferredType(out var preferredType) && allObjects.TryGetFirst(o => preferredType.IsInstanceOfType(o), out var preferredTarget))
                {
                    ValueEntry.WeakSmartValue = preferredTarget;
                    Property.MarkSerializationRootDirty();
                }
                else if (targetGameObject != null && allObjects.Count > 1)
                {
                    ShowSelector(fieldRect, targetGameObject, allObjects);
                }
                else
                {
                    ValueEntry.WeakSmartValue = allObjects.FirstOrDefault();
                    Property.MarkSerializationRootDirty();
                }
            }
        }

        private bool TryGetPreferredType(out Type type)
        {
            if (_preferredTypeResolver != null && !_preferredTypeResolver.HasError)
            {
                type = _preferredTypeResolver.GetValue();
                return type != null;
            }

            if (Attribute.PreferCurrentType)
            {
                Object prevValue = ValueEntry.SmartValue != null ? ValueEntry.SmartValue : null;
                if (prevValue != null)
                {
                    type = prevValue.GetType();
                    return true;
                }
            }

            type = null;
            return false;
        }

        private IEnumerable<Object> GetAllObjects(Object target)
        {
            if(target == null) yield break;
            var gameObject = target is Component cmp ? cmp.gameObject : target as GameObject;
            if(gameObject == null) yield break;
            
            if (Property.ValueEntry.BaseValueType.IsInstanceOfType(gameObject) && FilterObject(gameObject))
            {
                yield return gameObject;
            }
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if(FilterObject(component))
                    yield return component;
            }
        }

        private bool FilterObject(Object target)
        {
            var fieldType = Property.ValueEntry.BaseValueType;
            var isInstanceOfFieldType = fieldType.IsInstanceOfType(target);
            if (_elementConditionResolver == null || _elementConditionResolver.HasError) return isInstanceOfFieldType;
            _elementConditionResolver.Context.NamedValues.Set("value", target);
            return isInstanceOfFieldType && _elementConditionResolver.GetValue();
        }

        private void ShowSelector(Rect rect, GameObject targetGameObject, IEnumerable<Object> availableObjects = null)
        {
            availableObjects ??= GetAllObjects(targetGameObject);
            var items = availableObjects.Select(c => new GenericSelectorItem<Object>(c.GetType().Name, c)).ToList();

            var title = targetGameObject != null ? $"Select '{targetGameObject.name}' as" : "Select";
            var selector = new GenericSelector<Object>(title, false, items);
            selector.SetSelection((Component) null);
            selector.EnableSingleClickToSelect();
            selector.FlattenedTree = true;
            selector.SelectionTree.Config.DrawSearchToolbar = false;
            selector.SelectionTree.Selection.SupportsMultiSelect = false;

            selector.SelectionTree.EnumerateTree().AddThumbnailIcons();
            selector.SelectionConfirmed += selection =>
            {
                Property.ValueEntry.WeakSmartValue = selection.FirstOrDefault();
                Property.MarkSerializationRootDirty();
            };
            
            var height = items.Count * 23 + 23;
            
            selector.ShowInPopup(rect, new Vector2(rect.width, height));
        }
    }
}
