// Author: František Holubec
// Created: 14.09.2026

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    internal class AndroidKeystoreDialog : OdinEditorWindow
    {
        [ShowInInspector]
        [HideLabel]
        [InlineProperty]
        [HideReferenceObjectPicker]
        private AndroidSigningSettings _settings;

        private string _error;

        // Not modal, a modal window blocks the alias dropdown popup.
        public static CustomYieldInstruction Show(string reason, AndroidSigningSettings settings)
        {
            var window = CreateInstance<AndroidKeystoreDialog>();
            window.titleContent = new GUIContent("Android Keystore");
            window._error = reason;
            window._settings = settings;
            var size = new Vector2(460, 260);
            window.minSize = window.maxSize = size;
            window.position = CenterOnEditor(size);
            window.ShowUtility();
            return new WaitWhile(() => window != null);
        }

        [PropertyOrder(-100)]
        [OnInspectorGUI]
        private void DrawError()
        {
            if (!string.IsNullOrEmpty(_error))
                EditorGUILayout.HelpBox(_error, MessageType.Error);
        }

        [PropertyOrder(10)]
        [ButtonGroup]
        [Button("Cancel")]
        private void Cancel() => Close();

        [PropertyOrder(10)]
        [ButtonGroup]
        [Button("Continue")]
        private void Continue()
        {
            if (_settings.TryResolve(out _, out _error))
                Close();
        }

        private static Rect CenterOnEditor(Vector2 size)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            return new Rect(main.center.x - size.x * 0.5f, main.center.y - size.y * 0.5f, size.x, size.y);
        }
    }
}
