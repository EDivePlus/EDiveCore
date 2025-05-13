// Author: František Holubec
// Created: 13.05.2025

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public class NativeWrapperOdinEditor<T, TEditor> : OdinEditor
        where T : Object
        where TEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _unityEditor;

        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (var property in Tree.EnumerateTree())
            {
                if (property.Info.TypeOfOwner.IsAssignableFrom(typeof(T)))
                    property.State.Visible = false;
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
            if (_unityEditor == null)
                _unityEditor = CreateEditor(targets, typeof(TEditor));

            if (_unityEditor != null)
                _unityEditor.OnInspectorGUI();

            GUILayout.Space(8);
            base.OnInspectorGUI();
        }
    }
}
#endif
