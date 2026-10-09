// Author: František Holubec
// Created: 13.05.2025

#if UNITY_EDITOR
using System;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using CustomEditorUtility = EDIVE.EditorUtils.CustomEditorUtility;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor
{
    public enum BaseEditorDrawMode
    {
        NativeEditor,
        OdinEditor,
        Hidden
    }
    
    public abstract class NativeWrapperOdinEditor : OdinEditor
    {
        private const BindingFlags METHOD_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly PropertyInfo REFERENCE_TARGET_INDEX = typeof(UnityEditor.Editor).GetProperty("referenceTargetIndex", METHOD_FLAGS);

        private UnityEditor.Editor _unityEditor;
        private MethodInfo _sceneGUIMethod;
        private MethodInfo _hasFrameBoundsMethod;
        private MethodInfo _getFrameBoundsMethod;
        private bool _isTreePrepared;
        
        protected abstract Type BaseType { get; }
        protected abstract Type BaseEditorType { get; }
        
        protected virtual bool HideBaseFields => true;
        protected virtual BaseEditorDrawMode BaseEditorDrawMode => BaseEditorDrawMode.NativeEditor;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Created right away, native editors register scene handles and overlays in their OnEnable
            CreateUnityEditor();
        }

        protected override void OnDisable()
        {
            if (_unityEditor != null)
                DestroyImmediate(_unityEditor);
            base.OnDisable();
        }

        private void CreateUnityEditor()
        {
            if (_unityEditor != null || BaseEditorType == null)
                return;

            _unityEditor = CreateEditor(targets, BaseEditorType);
            if (_unityEditor == null)
                return;

            var editorType = _unityEditor.GetType();
            _sceneGUIMethod = FindMethod(editorType, "OnSceneGUI");
            _hasFrameBoundsMethod = FindMethod(editorType, "HasFrameBounds");
            _getFrameBoundsMethod = FindMethod(editorType, "OnGetFrameBounds");
        }

        // Private methods of base types are not returned for the derived type
        private static MethodInfo FindMethod(Type type, string name)
        {
            for (; type != null && type != typeof(UnityEditor.Editor); type = type.BaseType)
            {
                var method = type.GetMethod(name, METHOD_FLAGS | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                if (method != null)
                    return method;
            }
            return null;
        }

        public override void OnInspectorGUI()
        {
            if (BaseEditorType == null)
            {
                base.OnInspectorGUI();
                return;
            }

            if (BaseEditorDrawMode == BaseEditorDrawMode.NativeEditor)
            {
                CreateUnityEditor();
                if (_unityEditor != null)
                    _unityEditor.OnInspectorGUI();

                GUILayout.Space(4);
            }

            // Not base.OnInspectorGUI, it falls back to the default inspector when the target has no Odin members,
            // which draws the base members twice and skips the root drawers with validation
            DrawTree();
        }

        // Odin draws validation and members added on top of the base type, the native editor draws the rest
        protected override void DrawTree()
        {
            Tree.DrawMonoScriptObjectField = false;
            if (HideBaseFields && BaseEditorDrawMode != BaseEditorDrawMode.OdinEditor)
            {
                // Properties are created lazily, the first draw needs them before hiding, later draws update the tree themselves
                if (!_isTreePrepared)
                {
                    Tree.UpdateTree();
                    _isTreePrepared = true;
                }

                foreach (var property in Tree.RootProperty.Children)
                {
                    if (IsBaseMember(property))
                        property.State.Visible = false;
                }
            }
            base.DrawTree();
        }

        // Declared on the base type or above, groups count by their members
        private bool IsBaseMember(InspectorProperty property)
        {
            if (property.Info.PropertyType != PropertyType.Group)
                return property.Info.TypeOfOwner != null && property.Info.TypeOfOwner.IsAssignableFrom(BaseType);

            foreach (var child in property.Children)
            {
                if (!IsBaseMember(child))
                    return false;
            }
            return true;
        }

        // Unity calls these on the wrapper only, forwarded to the native editor

        protected virtual void OnSceneGUI()
        {
            if (_unityEditor == null || _sceneGUIMethod == null)
                return;

            // Multi-object scene GUI runs once per target
            REFERENCE_TARGET_INDEX?.SetValue(_unityEditor, REFERENCE_TARGET_INDEX.GetValue(this));
            _sceneGUIMethod.Invoke(_unityEditor, null);
        }

        protected virtual bool HasFrameBounds() => _unityEditor != null && _hasFrameBoundsMethod != null && (bool) _hasFrameBoundsMethod.Invoke(_unityEditor, null);

        protected virtual Bounds OnGetFrameBounds() => _getFrameBoundsMethod != null ? (Bounds) _getFrameBoundsMethod.Invoke(_unityEditor, null) : default;

        public override bool RequiresConstantRepaint() => base.RequiresConstantRepaint() || (_unityEditor != null && _unityEditor.RequiresConstantRepaint());

        public override bool HasPreviewGUI() => _unityEditor != null ? _unityEditor.HasPreviewGUI() : base.HasPreviewGUI();

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (_unityEditor != null) _unityEditor.OnPreviewGUI(rect, background);
            else base.OnPreviewGUI(rect, background);
        }

        public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
        {
            if (_unityEditor != null) _unityEditor.OnInteractivePreviewGUI(rect, background);
            else base.OnInteractivePreviewGUI(rect, background);
        }

        public override void OnPreviewSettings()
        {
            if (_unityEditor != null) _unityEditor.OnPreviewSettings();
            else base.OnPreviewSettings();
        }

        public override GUIContent GetPreviewTitle() => _unityEditor != null ? _unityEditor.GetPreviewTitle() : base.GetPreviewTitle();

        public override string GetInfoString() => _unityEditor != null ? _unityEditor.GetInfoString() : base.GetInfoString();
    }
    
    public abstract class NativeWrapperOdinEditor<TBase> : NativeWrapperOdinEditor
        where TBase : Object
    {
        protected override Type BaseType => typeof(TBase);
    }
    
    public abstract class NativeWrapperOdinEditor<TBase, TEditor> : NativeWrapperOdinEditor<TBase>
        where TBase : Object
        where TEditor : UnityEditor.Editor
    {
        protected override Type BaseEditorType => typeof(TEditor);
    }
    
    public abstract class AutoNativeWrapperOdinEditor : NativeWrapperOdinEditor
    {
        private Type _baseEditorType;
        protected override Type BaseEditorType => _baseEditorType ??= CustomEditorUtility.GetCustomEditorType(target.GetType(), GetType());
    }
}
#endif
