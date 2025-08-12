// Author: František Holubec
// Created: 08.04.2025

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.External.ToolbarExtensions;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using ParrelSync;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.External.ParrelSync
{
    public class ParrelSyncToolbarExtensions
    {
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            ToolbarExtender.AddToRightToolbar(OnToolbarGUI, -1000);
        }

        private static void OnToolbarGUI()
        {
            GUILayout.Space(2);
            var buttonLabel = ClonesManager.IsClone()
                ? GUIHelper.TempContent($" {ParrelSyncUtility.SelfArgumentsBundle.Data.Name}", FontAwesomeEditorIcons.CloneSolid.Active, "Parrel Sync (Clone)")
                : GUIHelper.TempContent(" Master", FontAwesomeEditorIcons.CrownSolid.Active, "Parrel Sync (Master)");

            if (ClonesManager.IsClone())
            {
                GUILayout.Label(buttonLabel,ToolbarStyles.ToolbarButton, GUILayout.ExpandWidth(false));
            }
            else
            {
                if (GUILayout.Button(buttonLabel, ToolbarStyles.ToolbarDropdown, GUILayout.ExpandWidth(false)))
                {
                    var dropdown = new ParrelSyncToolbarDropdown();
                    OdinEditorWindow.InspectObjectInDropDown(dropdown, new Vector2(0, 18), 420);
                }
            }

            GUILayout.Space(2);
        }

        [Serializable]
        private class ParrelSyncToolbarDropdown
        {
            [InfoBox("No clones found", InfoMessageType.Info, "@$value.Count == 0")]
            [EnhancedTableList(HideToolbar = true, IsReadOnly =  true)]
            [SerializeField]
            private List<DropdownProjectRecord> _ProjectCloneRecords;

            public ParrelSyncToolbarDropdown()
            {
                _ProjectCloneRecords = ParrelSyncUtility.CloneRecords.Select(r => new DropdownProjectRecord(r)).ToList();
            }
            
            [Button]
            private void OpenManager()
            {
                EnhancedClonesManagerWindow.OpenWindow();
            }
            
            [Serializable]
            private class DropdownProjectRecord
            {
                [ShowInInspector]
                [OnValueChanged(nameof(SaveData))]
                [EnhancedTableColumn(120)]
                private string Name { get => Arguments.Name; set => Arguments.Name = value; }

                [ShowInInspector]
                [OnValueChanged(nameof(SaveData))]
                [EnhancedTableColumn(80)]
                private bool SyncPlay { get => Arguments.SyncPlay; set => Arguments.SyncPlay = value; }

                [ShowInInspector]
                [OnValueChanged(nameof(SaveData))]
                [EnhancedTableColumn(80)]
                private bool SyncStop { get => Arguments.SyncStop; set => Arguments.SyncStop = value; }

                private bool IsRunning => ClonesManager.IsCloneProjectRunning(_projectCloneRecord.ProjectPath);

                private ProjectCloneRecord _projectCloneRecord;
                private SyncArgumentsBundle Arguments => _projectCloneRecord.ArgumentsBundle.Data;

                public DropdownProjectRecord(ProjectCloneRecord projectCloneRecord)
                {
                    _projectCloneRecord = projectCloneRecord;
                }

                private void SaveData() => _projectCloneRecord.ArgumentsBundle.SaveData();

                [EnhancedTableColumn("Running", 60)]
                [OnInspectorGUI]
                private void DrawRunning()
                {
                    GUIHelper.PushColor(IsRunning ? Color.green : Color.red);
                    GUILayout.Label(IsRunning ? FontAwesomeEditorIcons.SquareCheckSolid.Highlighted : FontAwesomeEditorIcons.SquareXmarkSolid.Highlighted, GUILayout.Height(18));
                    GUIHelper.PopColor();
                }

                [Button]
                [VerticalGroup("Action")]
                [ShowIf(nameof(IsRunning))]
                private void Focus()
                {
                    ParrelSyncUtility.FocusUnityEditor(_projectCloneRecord.ProjectPath);
                }

                [Button]
                [VerticalGroup("Action")]
                [HideIf(nameof(IsRunning))]
                private void Start()
                {
                    ClonesManager.OpenProject(_projectCloneRecord.ProjectPath);
                }
            }
        }
    }
}
#endif
