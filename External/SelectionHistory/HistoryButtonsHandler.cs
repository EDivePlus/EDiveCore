#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace EDIVE.External.SelectionHistory
{
    public static class HistoryButtonsHandler
    {
        private static readonly List<ButtonPressAction> LISTENERS = new List<ButtonPressAction>();

        private const int BACKWARD_BUTTON_CODE = 0x05;
        private const int FORWARD_BUTTON_CODE = 0x06;
        
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
#if UNITY_EDITOR_WIN
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
#endif
        }

        public static void AddListener(Action<HistoryButtonType> action, int priority = 0, bool preventOther = true)
        {
            LISTENERS.Add(new ButtonPressAction(action, priority, preventOther));
            LISTENERS.Sort();
        }

        public static void RemoveListener(Action<HistoryButtonType> action)
        {
            for (var index = LISTENERS.Count - 1; index >= 0; index--)
            {
                if (LISTENERS[index].Action == action) 
                    LISTENERS.Remove(LISTENERS[index]);
            }
        }

        private static void InvokeListeners(HistoryButtonType buttonType)
        {
            //Invoke only if unity has focus
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) 
                return;

            foreach (var listener in LISTENERS)
            {
                if (listener == null) continue;
                try
                {
                    listener.Action?.Invoke(buttonType);
                    if (listener.PreventOther) break;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKeyCode);
        
        private static void OnEditorUpdate()
        {
            var backwardButtonState = GetAsyncKeyState(BACKWARD_BUTTON_CODE);
            var forwardButtonState = GetAsyncKeyState(FORWARD_BUTTON_CODE);

            if ((backwardButtonState & 0x01) > 0) InvokeListeners(HistoryButtonType.Backward);
            if ((forwardButtonState & 0x01) > 0) InvokeListeners(HistoryButtonType.Forward);
        }
#endif
        
        private class ButtonPressAction : IComparable<ButtonPressAction>
        {
            private readonly int _priority;
            public bool PreventOther { get; }
            public Action<HistoryButtonType> Action { get; }

            public ButtonPressAction(Action<HistoryButtonType> action, int priority, bool preventOther)
            {
                _priority = priority;
                Action = action;
                PreventOther = preventOther;
            }

            public int CompareTo(ButtonPressAction other)
            {
                return _priority.CompareTo(other._priority);
            }
        }
    }

    public enum HistoryButtonType
    {
        Forward,
        Backward
    }
}
#endif
