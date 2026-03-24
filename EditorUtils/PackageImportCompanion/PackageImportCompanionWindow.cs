// Author: František Holubec
// Created: 24.03.2026

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils.PackageImportCompanion
{
    public class PackageImportCompanionWindow : OdinEditorWindow
    {
        private static readonly Dictionary<int, PackageImportCompanionWindow> ACTIVE_COMPANIONS = new();
		
        [HideInInspector]
        [SerializeField] 
        private EditorWindow _ImportWindow;

        [HideInInspector]
        [SerializeField] 
        private string[] _OriginalPaths;

        public static void ShowForImportWindow(EditorWindow importWindow)
        {
            var id = importWindow.GetInstanceID();
			
            ClearStaleEntries();
            if (ACTIVE_COMPANIONS.TryGetValue(id, out var existing) && existing != null)
                return;
            
            var companion = CreateInstance<PackageImportCompanionWindow>();
            companion.titleContent = new GUIContent("Package Import Companion");
            companion._ImportWindow = importWindow;
            companion._OriginalPaths = PackageImportUtility.GetImportItemPaths(importWindow);
            companion.ShowUtility();
            companion.PositionNearImportWindow();
            ACTIVE_COMPANIONS[id] = companion;
        }

        private static void ClearStaleEntries()
        {
            var staleKeys = new List<int>();
            foreach (var (id, window) in ACTIVE_COMPANIONS)
            {
                if (window == null || window._ImportWindow == null)
                    staleKeys.Add(id);
            }
            foreach (var key in staleKeys)
            {
                ACTIVE_COMPANIONS.Remove(key);
            }
        }
        
        private void PositionNearImportWindow()
        {
            if (_ImportWindow == null) 
                return;
			
            var importPos = _ImportWindow.position;
            position = new Rect(importPos.x + importPos.width + 10, importPos.y, 300,200);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_ImportWindow != null)
                ACTIVE_COMPANIONS[_ImportWindow.GetInstanceID()] = this;
        }
				
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_ImportWindow != null) 
                ACTIVE_COMPANIONS.Remove(_ImportWindow.GetInstanceID());
        }

        private void Update()
        {
            if (_ImportWindow == null) 
                Close();
        }
        
        [SerializeField]
        private string _ReplacePath = "Assets/Plugins/";
        
        [EnableGUI]
        [ShowInInspector] 
        private string ProjectSelectedFolder => PackageImportUtility.GetCurrentFolderPath();
		
        private bool HasProjectSelectedFolder => !string.IsNullOrEmpty(ProjectSelectedFolder);
        
        [Button]
        [EnableIf(nameof(HasProjectSelectedFolder))]
        private void ReplaceWithProjectSelectedFolder()
        {
            var selectedFolder = ProjectSelectedFolder;
            if (_ImportWindow == null || string.IsNullOrEmpty(selectedFolder)) 
                return;
            
            var newPaths = _OriginalPaths.Select(p => ReplaceRoot(p, _ReplacePath, selectedFolder)).ToArray();
            PackageImportUtility.SetImportWindowPaths(_ImportWindow, newPaths);
        }

        [Button]
        private void PickFolderAndReplace()
        {
            var absolutePath = EditorUtility.OpenFolderPanel("Select target folder", "Assets", "");
            if (string.IsNullOrEmpty(absolutePath)) return;
            if (_ImportWindow == null) return;

            absolutePath = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');

            string relativePath;
            if (absolutePath == dataPath)
            {
                relativePath = "Assets";
            }
            else if (absolutePath.StartsWith(dataPath + "/"))
            {
                relativePath = "Assets" + absolutePath[dataPath.Length..];
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Folder",
                    "Please select a folder inside the Assets directory.", "OK");
                return;
            }
            
            var newPaths = _OriginalPaths.Select(p => ReplaceRoot(p, _ReplacePath, relativePath)).ToArray();
            PackageImportUtility.SetImportWindowPaths(_ImportWindow, newPaths);
        }

        private static string ReplaceRoot(string path, string oldRoot, string newRoot)
        {
            path = path.Replace("\\", "/");
            oldRoot = oldRoot.Replace("\\", "/").TrimEnd('/');
            newRoot = newRoot.Replace("\\", "/").TrimEnd('/');

            if (path == oldRoot)
                return newRoot;

            if (path.StartsWith($"{oldRoot}/"))
                return newRoot + path[oldRoot.Length..];

            return path;
        }
        
    }
}
