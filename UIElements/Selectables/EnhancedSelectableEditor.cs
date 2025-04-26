// Author: František Holubec
// Created: 25.04.2025

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.Selectables
{
    public class EnhancedSelectableEditor<T, TEditor> : OdinEditor
        where T : Selectable
        where TEditor : SelectableEditor
    {
        private Editor _unityEditor;

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
