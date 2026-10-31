// Author: František Holubec
// Created: 31.10.2026

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.EditorTools.PackageImportCompanion
{
    // Target folder panel drawn inside Unity's Import Package window
    [InitializeOnLoad]
    public static class PackageImportFolderBar
    {
        private const string PANEL_NAME = "EDIVE-PackageImportFolderPanel";
        private const string FROM_ROOT_KEY = "EDIVE.PackageImport.FromRoot";
        private const string DEFAULT_FROM_ROOT = "Assets/Plugins";
        private const int PANEL_WIDTH = 190;

        private static readonly Dictionary<int, string> TARGETS = new();

        static PackageImportFolderBar()
        {
            EditorApplication.update -= AttachToActiveWindow;
            EditorApplication.update += AttachToActiveWindow;
        }

        private static void AttachToActiveWindow()
        {
            TryAttach(EditorWindow.focusedWindow);
            TryAttach(EditorWindow.mouseOverWindow);
        }

        private static void TryAttach(EditorWindow window)
        {
            if (!PackageImportUtility.IsImportWindow(window) || window.rootVisualElement.Q(PANEL_NAME) != null)
                return;

            var windowGUI = window.rootVisualElement.parent?.Children().OfType<IMGUIContainer>().FirstOrDefault();
            if (windowGUI == null)
                return;

            // Unity's own GUI moves left to make room for the panel
            windowGUI.style.position = Position.Absolute;
            windowGUI.style.top = 0;
            windowGUI.style.left = 0;
            windowGUI.style.right = PANEL_WIDTH;
            windowGUI.style.bottom = 0;

            var panel = new VisualElement {name = PANEL_NAME};
            panel.style.position = Position.Absolute;
            panel.style.top = 0;
            panel.style.right = 0;
            panel.style.bottom = 0;
            panel.style.width = PANEL_WIDTH;
            panel.style.paddingTop = 8;
            panel.style.paddingLeft = 6;
            panel.style.paddingRight = 6;
            panel.style.borderLeftWidth = 1;
            panel.style.borderLeftColor = new Color(0f, 0f, 0f, 0.3f);
            panel.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);
            panel.Add(new IMGUIContainer(() => DrawPanel(window)));
            window.rootVisualElement.Add(panel);
        }

        private static void DrawPanel(EditorWindow window)
        {
            var id = window.GetInstanceID();
            TARGETS.TryGetValue(id, out var target);
            var selectedFolder = PackageImportUtility.GetCurrentFolderPath();

            GUILayout.Label("Move", EditorStyles.boldLabel);
            var fromRoot = EditorGUILayout.DelayedTextField(EditorPrefs.GetString(FROM_ROOT_KEY, DEFAULT_FROM_ROOT));
            EditorPrefs.SetString(FROM_ROOT_KEY, fromRoot);

            GUILayout.Label("To", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField(target ?? "package paths");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(selectedFolder)))
                {
                    if (GUILayout.Button(new GUIContent("Selected", selectedFolder ?? "Select a folder in the Project window")))
                        Apply(window, fromRoot, selectedFolder);
                }

                if (GUILayout.Button("Pick..."))
                    EditorApplication.delayCall += () => PickFolder(window, fromRoot);
            }

            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Reset"))
                    Apply(window, fromRoot, null);
            }
        }

        private static void PickFolder(EditorWindow window, string fromRoot)
        {
            var absolutePath = EditorUtility.OpenFolderPanel("Select target folder", "Assets", "");
            if (string.IsNullOrEmpty(absolutePath) || window == null)
                return;

            absolutePath = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (absolutePath != dataPath && !absolutePath.StartsWith(dataPath + "/"))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside the Assets directory.", "OK");
                return;
            }

            Apply(window, fromRoot, "Assets" + absolutePath[dataPath.Length..]);
        }

        private static void Apply(EditorWindow window, string fromRoot, string target)
        {
            PackageImportUtility.RemapImportWindow(window, fromRoot, target);
            if (target == null)
                TARGETS.Remove(window.GetInstanceID());
            else
                TARGETS[window.GetInstanceID()] = target;
        }
    }
}
