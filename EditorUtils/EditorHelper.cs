using System;
using UnityEditor;

namespace EDIVE.EditorUtils
{
    public static class EditorHelper
    {
        // Can be access from different threads!
        public static bool IsInPlayMode { get; private set; }

        [InitializeOnLoadMethod]
        private static void InitializeEditorHelper()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            IsInPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            IsInPlayMode = state switch
            {
                PlayModeStateChange.EnteredPlayMode or PlayModeStateChange.ExitingEditMode => true,
                PlayModeStateChange.EnteredEditMode or PlayModeStateChange.ExitingPlayMode => false,
                _ => throw new System.ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
        
        public static void ExecuteNextFrame(Action callback)
        {
            EditorApplication.update += ExecuteNextFrameCallback;
            return;

            void ExecuteNextFrameCallback()
            {
                callback?.Invoke();
                EditorApplication.update -= ExecuteNextFrameCallback;
            }
        }
    }
}

