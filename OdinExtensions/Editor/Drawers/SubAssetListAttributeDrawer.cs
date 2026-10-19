// Author: Michal Petr
// Created: 19.10.2026

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.EditorUtils.SubAssets;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class SubAssetListAttributeDrawer<T> : OdinAttributeDrawer<SubAssetListAttribute, T>, IDisposable
    {
        private ICollectionResolver _resolver;
        private ScriptableObject _pendingAdd;
        private readonly List<Object> _pendingRemove = new();

        protected override bool CanDrawAttributeValueProperty(InspectorProperty property)
        {
            return property.ChildResolver is ICollectionResolver resolver && typeof(ScriptableObject).IsAssignableFrom(resolver.ElementType);
        }

        protected override void Initialize()
        {
            _resolver = (ICollectionResolver) Property.ChildResolver;
            _resolver.OnBeforeChange += OnBeforeChange;
            _resolver.OnAfterChange += OnAfterChange;
        }

        public void Dispose()
        {
            if (_resolver == null) return;
            _resolver.OnBeforeChange -= OnBeforeChange;
            _resolver.OnAfterChange -= OnAfterChange;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (!TryGetRootAsset(out _))
            {
                SirenixEditorGUI.ErrorMessageBox("Sub-asset list only works on a saved asset.");
                CallNextDrawer(label);
                return;
            }

            var oldAddFunction = CollectionDrawerStaticInfo.NextCustomAddFunction;
            CollectionDrawerStaticInfo.NextCustomAddFunction = OpenAddDropdown;
            CallNextDrawer(label);
            CollectionDrawerStaticInfo.NextCustomAddFunction = oldAddFunction;

            if (_pendingAdd != null)
            {
                _resolver.QueueAdd(new object[] {_pendingAdd});
                _pendingAdd = null;
            }
        }

        private bool TryGetRootAsset(out Object root)
        {
            root = Property.Tree.WeakTargets.Count == 1 ? Property.Tree.WeakTargets[0] as Object : null;
            return root != null && AssetDatabase.IsMainAsset(root);
        }

        private void OpenAddDropdown()
        {
            OdinExtensionUtils.DrawSubtypeDropDownOrCall(_resolver.ElementType, CreateSubAsset);
        }

        private void CreateSubAsset(Type type)
        {
            if (type == null || !TryGetRootAsset(out var root)) return;

            var existingNames = SubAssetUtility.GetAllSubAssets(root).Where(x => x != null).Select(x => x.name).ToArray();
            var instance = ScriptableObject.CreateInstance(type);
            instance.name = ObjectNames.GetUniqueName(existingNames, ObjectNames.NicifyVariableName(type.Name));
            SubAssetUtility.AddSubAsset(root, instance);

            _pendingAdd = instance;
            GUIHelper.RequestRepaint();
        }

        private void OnBeforeChange(CollectionChangeInfo info)
        {
            var list = ValueEntry.WeakValues[info.SelectionIndex] as IList;
            switch (info.ChangeType)
            {
                case CollectionChangeType.RemoveIndex:
                    if (list != null && info.Index >= 0 && info.Index < list.Count)
                        _pendingRemove.Add(list[info.Index] as Object);
                    break;
                case CollectionChangeType.RemoveValue:
                    _pendingRemove.Add(info.Value as Object);
                    break;
                case CollectionChangeType.Clear:
                    if (list != null)
                        _pendingRemove.AddRange(list.OfType<Object>());
                    break;
            }
        }

        private void OnAfterChange(CollectionChangeInfo info)
        {
            if (_pendingRemove.Count == 0) return;

            var removed = _pendingRemove.Where(x => x != null).ToList();
            _pendingRemove.Clear();

            // Delayed, a drag reorder removes and inserts the same element
            EditorApplication.delayCall += () => DeleteRemovedSubAssets(removed);
        }

        private void DeleteRemovedSubAssets(List<Object> removed)
        {
            if (!TryGetRootAsset(out var root)) return;

            var list = ValueEntry.WeakValues[0] as IList;
            foreach (var asset in removed)
            {
                if (asset == null || asset == root || (list != null && list.Contains(asset))) continue;
                if (!SubAssetUtility.IsSubAssetOf(asset, root)) continue;
                SubAssetUtility.DeleteSubAsset(asset);
            }
        }
    }

    public class SubAssetListElementDrawer<T> : OdinAttributeDrawer<SubAssetListAttribute, T>, IDisposable where T : ScriptableObject
    {
        private LocalPersistentContext<bool> _isExpanded;
        private UnityEditor.Editor _editor;

        protected override bool CanDrawAttributeValueProperty(InspectorProperty property)
        {
            return property.Parent?.ChildResolver is ICollectionResolver;
        }

        protected override void Initialize()
        {
            _isExpanded = this.GetPersistentValue("IsExpanded", false);
        }

        public void Dispose()
        {
            if (_editor == null) return;
            Object.DestroyImmediate(_editor);
            _editor = null;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var value = ValueEntry.SmartValue;
            if (value == null)
            {
                CallNextDrawer(label);
                return;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(20));
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(15));
            _isExpanded.Value = SirenixEditorGUI.Foldout(_isExpanded.Value, (string) null, SirenixGUIStyles.Foldout);
            EditorGUILayout.EndHorizontal();

            var newName = SirenixEditorFields.DelayedTextField((GUIContent) null, value.name);
            if (newName != value.name && !string.IsNullOrWhiteSpace(newName))
            {
                value.name = newName;
                EditorUtility.SetDirty(value);
                AssetDatabase.SaveAssets();
            }

            var pingRect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(pingRect, FontAwesomeEditorIcons.CrosshairsSimpleRegular, "Select sub-asset"))
                EditorGUIUtility.PingObject(value);
            EditorGUILayout.EndHorizontal();

            if (SirenixEditorGUI.BeginFadeGroup(this, _isExpanded.Value))
            {
                SirenixEditorGUI.BeginBox();
                DrawInlineEditor(value);
                SirenixEditorGUI.EndBox();
            }
            SirenixEditorGUI.EndFadeGroup();
        }

        private void DrawInlineEditor(T value)
        {
            UnityEditor.Editor.CreateCachedEditor(value, null, ref _editor);
            if (_editor == null) return;

            // Some editors break when the header is not drawn
            GUIHelper.BeginDrawToNothing();
            _editor.DrawHeader();
            GUIHelper.EndDrawToNothing();

            var showMonoScript = GlobalConfig<GeneralDrawerConfig>.Instance.ShowMonoScriptInEditor;
            GlobalConfig<GeneralDrawerConfig>.Instance.ShowMonoScriptInEditor = false;
            GUIHelper.PushHierarchyMode(false);
            EnhancedInlineEditorAttributeDrawer.CurrentInlineEditorDrawDepth++;
            try
            {
                _editor.OnInspectorGUI();
            }
            finally
            {
                EnhancedInlineEditorAttributeDrawer.CurrentInlineEditorDrawDepth--;
                GUIHelper.PopHierarchyMode();
                GlobalConfig<GeneralDrawerConfig>.Instance.ShowMonoScriptInEditor = showMonoScript;
            }
        }
    }
}
