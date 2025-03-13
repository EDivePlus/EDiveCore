#if UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public static class UnityFuckedUpOdinDropdownFixer
    {
        private static HashSet<EditorWindow> _previouslyOpenWindows = new();
        private static System.Reflection.PropertyInfo _activeWindowsProperty;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _activeWindowsProperty = typeof(EditorWindow).GetProperty("activeEditorWindows",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            EditorApplication.update += TrackOpenedWindows;
        }

        private static void TrackOpenedWindows()
        {
            if (_activeWindowsProperty == null || _activeWindowsProperty.GetValue(null) is not IList<EditorWindow> currentOpenWindows)
                return;

            foreach (var window in currentOpenWindows)
            {
                if (_previouslyOpenWindows.Add(window))
                    OnWindowOpened(window);
            }
        }

        private static void OnWindowOpened(EditorWindow window)
        {
            EditorApplication.update += Delayed;
            return;

            void Delayed()
            {
                if (window == null)
                {
                    EditorApplication.update -= Delayed;
                    return;
                }

                if (window.position.height == 0)
                {
                    var arrowDownEvent = new Event
                    {
                        type = EventType.KeyDown,
                        keyCode = KeyCode.LeftArrow
                    };
                    window.SendEvent(arrowDownEvent);
                    var arrowDownEventRevert = new Event
                    {
                        type = EventType.KeyDown,
                        keyCode = KeyCode.RightArrow
                    };
                    window.SendEvent(arrowDownEventRevert);
                    return;
                }

                EditorApplication.update -= Delayed;
            }
        }
    }
}
#endif
