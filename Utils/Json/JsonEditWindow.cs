#if UNITY_EDITOR
using System;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Utils.Json
{
    public class JsonEditWindow : OdinEditorWindow
    {
        private Func<string> _textGetter;
        private Action<string> _textSetter;
        private Vector2 _currentScrollPos;

        private string _originalJson;
        private string _json;
        private bool _changed;
        
        private bool AccessorsAssigned => _textGetter != null || _textSetter != null;
        
        private GUIStyle _buttonStyle;
        private GUIStyle ButtonStyle => _buttonStyle ??= new GUIStyle(SirenixGUIStyles.Button)
        {
            padding = new RectOffset(4, 4, 4, 4)
        };
        
        private GUIStyle _textAreaStyle;
        private GUIStyle TextAreaStyle => _textAreaStyle ??= new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true
        };

        
        public static void OpenWindow(Func<string> getter, Action<string> setter)
        {
            var window = GetWindow<JsonEditWindow>();
            window._textGetter = getter;
            window._textSetter = setter;
            window.Init();
            window.Show();
        }

        private void Init()
        {
            base.Initialize();
            _originalJson = _textGetter?.Invoke();
            _json = JsonUtils.PrettifyJsonString(_originalJson);
        }

        [OnInspectorGUI]
        private void DrawButtons()
        {
            if (!AccessorsAssigned)
            {
                SirenixEditorGUI.InfoMessageBox("No Accessors Assigned!");
            }

            GUIHelper.PushGUIEnabled(AccessorsAssigned);
            EditorGUILayout.BeginHorizontal();
            
            GUIHelper.PushGUIEnabled(_changed);
            if (ToolbarButton(FontAwesomeEditorIcons.FloppyDiskSolid, "Save"))
            {
                _textSetter?.Invoke(_json);
            }
            GUIHelper.PopGUIEnabled();
            
            if (ToolbarButton(FontAwesomeEditorIcons.RotateLeftRegular, "Reload"))
            {
                _json = _originalJson = _textGetter?.Invoke();
            }
            
            GUILayout.Space(16);
            
            if (ToolbarButton(FontAwesomeEditorIcons.CopySolid, "Copy"))
            {
                Clipboard.Copy(_json);
            }
            
            GUILayout.Space(16);
            
            if (ToolbarButton(FontAwesomeEditorIcons.LineHeightSolid, "Format Indented"))
            {
                _json = JsonUtils.PrettifyJsonString(_json);
            }

            if (ToolbarButton(FontAwesomeEditorIcons.ArrowsToLineSolid, "Format Simple"))
            {
                _json = JsonUtils.SimplifyJsonString(_json);
            }

            EditorGUILayout.EndHorizontal();
            GUIHelper.PopGUIEnabled();
        }

        private bool ToolbarButton(EditorIcon icon, string tooltip)
        {
            return GUILayout.Button(GUIHelper.TempContent(icon.Highlighted, tooltip), ButtonStyle, GUILayout.Width(34), GUILayout.Height(24));
        }
        
        [PropertySpace(4)]
        [OnInspectorGUI]
        private void DrawTextArea()
        {
            GUIHelper.PushGUIEnabled(AccessorsAssigned);
            EditorGUI.BeginChangeCheck();
            _currentScrollPos = EditorGUILayout.BeginScrollView(_currentScrollPos);
            var newJson = EditorGUILayout.TextArea(_json, TextAreaStyle);
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                _json = newJson;
                _changed = _json != _originalJson;
            }
            GUIHelper.PopGUIEnabled();
        }
    }
}
#endif
