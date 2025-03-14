// Author: František Holubec
// Created: 14.03.2025

#if PARREL_SYNC
using System;
using EDIVE.OdinExtensions.Attributes;
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
        [ShowOpenInExplorer]
        [SerializeField]
        [EnableGUI]
        [ReadOnly]
        private string _ProjectPath;
        public string ProjectPath => _ProjectPath;

        [ShowInInspector]
        [HideLabel]
        [InlineProperty]
        [BoxGroup("Clone Arguments")]
        [HideReferenceObjectPicker]
        public ParrelSyncArgumentsBundle ArgumentsBundle
        {
            get => TryGetArgumentsBundle(out var result) ? result : default;
            set => SetArgumentsBundle(value);
        }

        public ProjectCloneRecord(string projectPath)
        {
            _ProjectPath = projectPath;
        }

        public void SetArgumentsBundle(ParrelSyncArgumentsBundle argumentsBundle)
        {
            ParrelSyncUtility.SetArgumentsBundle(_ProjectPath, argumentsBundle);
        }

        public bool TryGetArgumentsBundle(out ParrelSyncArgumentsBundle result)
        {
            return ParrelSyncUtility.TryGetArgumentsBundle(_ProjectPath, out result);
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
            EditorGUI.BeginDisabledGroup(isEditorRunning);
            if(GUILayout.Button("Start", GUILayout.Width(80)))
            {
                ClonesManager.OpenProject(_ProjectPath);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
