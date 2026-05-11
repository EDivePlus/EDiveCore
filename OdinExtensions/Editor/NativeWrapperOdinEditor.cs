// Author: František Holubec
// Created: 13.05.2025

#if UNITY_EDITOR
using System;
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
        private UnityEditor.Editor _unityEditor;
        
        protected abstract Type BaseType { get; }
        protected abstract Type BaseEditorType { get; }
        
        protected virtual bool HideBaseFields => true;
        protected virtual BaseEditorDrawMode BaseEditorDrawMode => BaseEditorDrawMode.NativeEditor;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (BaseEditorDrawMode != BaseEditorDrawMode.OdinEditor)
            {
                foreach (var property in Tree.EnumerateTree())
                {
                    if (property.Info.TypeOfOwner.IsAssignableFrom(BaseType))
                        property.State.Visible = false;
                }
            }
        }

        protected override void OnDisable()
        {
            if (_unityEditor != null)
                DestroyImmediate(_unityEditor);
            base.OnDisable();
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
                if (_unityEditor == null)
                    _unityEditor = CreateEditor(targets, BaseEditorType);

                if (_unityEditor != null)
                    _unityEditor.OnInspectorGUI();
                
                GUILayout.Space(4);
            }

            base.OnInspectorGUI();
        }

        protected override void DrawTree()
        {
            Tree.DrawMonoScriptObjectField = false;
            base.DrawTree();
        }
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
