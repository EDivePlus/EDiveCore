// Author: František Holubec
// Created: 08.04.2025

#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using ParrelSync;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using EDIVE.EditorUtils;
using UnityEditor.Toolbars;

namespace EDIVE.ParrelSyncUtils
{
    public class ParrelSyncToolbarExtensions
    {
        [MainToolbarElement("EDive/Parrel Sync", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 10)]
        public static MainToolbarElement CreateToolbarButton()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var dropdown = new EditorToolbarDropdown();
                dropdown.AddToClassList("unity-editor-toolbar-element");
                var isClone = ClonesManager.IsClone();
                
                if (isClone)
                {
                    dropdown.text = $" {ParrelSyncUtility.SelfArgumentsBundle.Data.Name}";
                    dropdown.icon = FontAwesomeEditorIcons.CloneSolid.Raw;
                    dropdown.tooltip = "Parrel Sync (Clone)";
                    dropdown.SetEnabled(false); // acts like label
                }
                else
                {
                    dropdown.text = " Master";
                    dropdown.icon = FontAwesomeEditorIcons.CrownSolid.Raw;
                    dropdown.tooltip = "Parrel Sync (Master)";

                    dropdown.clicked += () =>
                    {
                        var window = new ParrelSyncToolbarDropdown();
                        OdinEditorWindow.InspectObjectInDropDown(window, dropdown.worldBound, 420);
                    };
                }

                return dropdown;
            });
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
