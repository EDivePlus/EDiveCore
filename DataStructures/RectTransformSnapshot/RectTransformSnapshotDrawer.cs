#if UNITY_EDITOR
using System;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.DataStructures.RectTransformSnapshot
{
    public class RectTransformSnapshotDrawer : OdinValueDrawer<RectTransformSnapshot>, IDisposable
    {
        private RectTransform _tempRectTransform;
        private Editor _editor;
        
        protected override void Initialize()
        {
            Property.Tree.OnUndoRedoPerformed += UpdateInternalRectTransformData;

            _tempRectTransform = new GameObject($"RECT_TRANSFORM_PRESET").AddComponent<RectTransform>();
            _tempRectTransform.gameObject.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

            if (Property.TryGetParentObject<MonoBehaviour>(out var instance))
            {
                var currentTransform = instance.transform;
                var parent = currentTransform.parent;
                if (parent == null)
                    parent = currentTransform;

                if (!PrefabUtility.IsPartOfPrefabAsset(parent))
                    _tempRectTransform.SetParent(parent);
            }

            UpdateInternalRectTransformData();

            var rectTransformEditorType = Type.GetType("UnityEditor.RectTransformEditor, UnityEditor");
            _editor = Editor.CreateEditor(_tempRectTransform, rectTransformEditorType);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        { 
            SirenixEditorGUI.BeginIndentedHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            _editor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
                CopyFromRectTransform(_tempRectTransform);
            EditorGUILayout.EndVertical();
            SirenixEditorGUI.EndIndentedHorizontal();
        }

        private void CopyFromRectTransform(RectTransform rectTransform)
        {
            Property.Parent.RecordForUndo(null, true);
            ValueEntry.SmartValue.FromRectTransform(rectTransform);
            Property.Parent.ValueEntry.ApplyChanges();
            Property.MarkSerializationRootDirty();
            ValueEntry.SmartValue.ApplyTo(_tempRectTransform);
        }

        private void UpdateInternalRectTransformData()
        {
            ValueEntry.SmartValue.ApplyTo(_tempRectTransform);
        }

        public void Dispose()
        {
            Property.Tree.OnUndoRedoPerformed -= UpdateInternalRectTransformData;

            Object.DestroyImmediate(_editor);
            if (_tempRectTransform)
                Object.DestroyImmediate(_tempRectTransform.gameObject);
        }
    }
}
#endif
