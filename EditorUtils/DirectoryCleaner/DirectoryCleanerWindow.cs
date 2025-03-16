using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils.DirectoryCleaner
{
    public class DirectoryCleanerWindow : OdinEditorWindow
    {
        [ShowInInspector]
        [LabelWidth(150)]
        private bool ClearOnEditorLoaded { get => DirectoryCleanerUtility.CleanOnEditorInitialized; set => DirectoryCleanerUtility.CleanOnEditorInitialized = value; }

        private bool AnyEmptyDirectories => EmptyDirectories != null && EmptyDirectories.Any();
        private bool AnySelectedDirectories => SelectedDirectories != null && SelectedDirectories.Any();

        [ShowInInspector]
        [PropertyOrder(10)]
        [CustomValueDrawer(nameof(CustomDirectoryDrawer))]
        [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true)]
        private List<DirectoryInfo> EmptyDirectories { get; set; } = new List<DirectoryInfo>();

        private HashSet<DirectoryInfo> SelectedDirectories { get; set; } = new HashSet<DirectoryInfo>();

        [Button]
        private void Refresh()
        {
            EmptyDirectories = DirectoryCleanerUtility.GetEmptyDirectories();
            RefreshSelected();

            if (EmptyDirectories.Any())
            {
                RemoveNotification();
            }
            else
            {
                ShowNotification(new GUIContent("No Empty Directory"));
            }
        }

        [PropertySpace]
        [Button]
        [HorizontalGroup("Delete", Order = 15)]
        [EnableIf(nameof(AnySelectedDirectories))]
        private void DeleteSelected()
        {
            DirectoryCleanerUtility.DeleteDirectories(SelectedDirectories);
            EmptyDirectories = DirectoryCleanerUtility.GetEmptyDirectories();
            RefreshSelected();
        }
        
        [PropertySpace]
        [Button]
        [HorizontalGroup("Delete", Order = 15)]
        [EnableIf(nameof(AnyEmptyDirectories))]
        private void DeleteAll()
        {
            DirectoryCleanerUtility.DeleteDirectories(EmptyDirectories);
            EmptyDirectories = DirectoryCleanerUtility.GetEmptyDirectories();
            RefreshSelected();
        }

        [MenuItem("NoxE/Directory Cleaner")]
        public static void ShowWindow()
        {
            GetWindow<DirectoryCleanerWindow>();
        }

        protected override void Initialize()
        {
            base.Initialize();
            titleContent = new GUIContent("Directory Cleaner", FontAwesomeEditorIcons.FolderGearSolid.Active);
        }

        private DirectoryInfo CustomDirectoryDrawer(DirectoryInfo value)
        {
            if (value == null || !value.Exists)
            {
                GUILayout.Label("Invalid path info");
                return value;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            var wasSelected = SelectedDirectories.Contains(value);
            var isSelected = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(25));
            if (EditorGUI.EndChangeCheck() && wasSelected != isSelected)
            {
                if (isSelected)
                    SelectedDirectories.Add(value);
                else
                    SelectedDirectories.Remove(value);
            }

            var relativePath = PathUtility.GetProjectRelativePath(value.FullName);
            SirenixEditorFields.TextField(relativePath);
            if (GUILayout.Button("Ping", GUILayout.ExpandWidth(false)))
            {
                var targetObj = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
                EditorGUIUtility.PingObject(targetObj);
                Selection.activeObject = targetObj;
            }

            if (GUILayout.Button("Show In Explorer", GUILayout.ExpandWidth(false)))
            {
                EditorUtility.RevealInFinder(value.FullName.ConvertPathSeparator());
            }

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            GUILayout.Space(8);
            if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
            {
                DirectoryCleanerUtility.DeleteDirectory(value);
                EmptyDirectories = DirectoryCleanerUtility.GetEmptyDirectories();
                RefreshSelected();
            }

            GUI.backgroundColor = previousColor;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private void RefreshSelected()
        {
            SelectedDirectories = new HashSet<DirectoryInfo>(EmptyDirectories.Where(d => SelectedDirectories.Any(s => d.FullName == s.FullName)));
        }
    }
}
