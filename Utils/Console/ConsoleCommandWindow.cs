#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EDIVE.Utils.Console
{
    public class ConsoleCommandWindow : EditorWindow
    {
        private const string INPUT_CONTROL = "CmdInput";

        private string _input = "";
        private string _logs = "";
        private Vector2 _scroll;
        private bool _focusInput;

        [MenuItem("Tools/Server Console")]
        public static void Open()
        {
            var window = GetWindow<ConsoleCommandWindow>("Server Console");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            ConsoleCommandHandler.OnLog += AppendLog;
            _focusInput = true;
        }

        private void OnDisable()
        {
            ConsoleCommandHandler.OnLog -= AppendLog;
        }

        private void AppendLog(string msg)
        {
            _logs += msg + "\n";
            _scroll.y = float.MaxValue;
            Repaint();
        }

        private void OnGUI()
        {
            var e = Event.current;
            var submitPressed = 
                e.type == EventType.KeyDown && 
                (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) &&
                GUI.GetNameOfFocusedControl() == INPUT_CONTROL;

            if (submitPressed)
            {
                Submit();
                e.Use();
                _focusInput = true;
                Repaint();
                return;
            }
            
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, 
                GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_logs, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Clear Logs"))
            {
                _logs = "";
            }

            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName(INPUT_CONTROL);
            _input = EditorGUILayout.TextField(_input);

            if (GUILayout.Button("Send", GUILayout.Width(60)))
            {
                Submit();
                _focusInput = true;
            }
            EditorGUILayout.EndHorizontal();
            
            if (_focusInput && e.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl(INPUT_CONTROL);
                _focusInput = false;
            }
        }

        private void Submit()
        {
            if (string.IsNullOrWhiteSpace(_input))
                return;

            _logs += $"> {_input}\n";
            _scroll.y = float.MaxValue;
            ConsoleCommandHandler.EnqueueCommand(_input.Trim());
            _input = "";
            GUI.FocusControl(null);
        }
    }
}
#endif