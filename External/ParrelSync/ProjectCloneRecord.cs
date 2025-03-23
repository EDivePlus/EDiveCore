// Author: František Holubec
// Created: 14.03.2025

#if PARREL_SYNC
using System;
using System.IO;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Json;
using ParrelSync;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.External.ParrelSync
{
    [Serializable]
    public class ProjectCloneRecord
    {
        [HideInInspector]
        [SerializeField]
        private string _ProjectPath;

        [EnableGUI]
        [ShowOpenInExplorer]
        [ShowInInspector]
        public string ProjectPath => _ProjectPath;

        public string ArgumentBundlePath => Path.Combine(_ProjectPath, ClonesManager.ArgumentFileName);

        [ShowInInspector]
        [HideReferenceObjectPicker]
        public JsonFileWatchingEditor<SyncArgumentsBundle> ArgumentsBundle
        {
            get => _argumentsBundleEditor ??= new JsonFileWatchingEditor<SyncArgumentsBundle>(ArgumentBundlePath, ParrelSyncUtility.JSON_SERIALIZER_SETTINGS);
            set => _argumentsBundleEditor = value;
        }
        private JsonFileWatchingEditor<SyncArgumentsBundle> _argumentsBundleEditor;

        public ProjectCloneRecord(string projectPath)
        {
            _ProjectPath = projectPath;
        }

        [PropertySpace(0, 6)]
        [PropertyOrder(-10)]
        [OnInspectorGUI]
        private void DrawEditorRunning()
        {
            var isEditorRunning = ClonesManager.IsCloneProjectRunning(_ProjectPath);
            EditorGUILayout.BeginHorizontal();
            GUIHelper.PushColor(isEditorRunning ? Color.green : Color.red);
            GUILayout.Label(isEditorRunning ? "Running" : "Not running");
            GUIHelper.PopColor();

            GUILayout.FlexibleSpace();
            if (isEditorRunning)
            {
                if (GUILayout.Button("Focus", GUILayout.Width(80)))
                {
                    ParrelSyncUtility.FocusUnityEditor(_ProjectPath);
                }
            }
            else
            {
                if (GUILayout.Button("Start", GUILayout.Width(80)))
                {
                    ClonesManager.OpenProject(_ProjectPath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
