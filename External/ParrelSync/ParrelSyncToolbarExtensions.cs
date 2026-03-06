// Author: František Holubec
// Created: 08.04.2025

#if UNITY_6000_3_OR_NEWER
#define UNITY_6_TOOLBAR
#endif

#if UNITY_EDITOR
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

#if UNITY_6_TOOLBAR
using EDIVE.EditorUtils;
using UnityEditor.Toolbars;
#else
using UnityEditor;
using EDIVE.External.ToolbarExtensions;
#endif

namespace EDIVE.External.ParrelSync
{
    public class ParrelSyncToolbarExtensions
    {
#if UNITY_6_TOOLBAR
        [MainToolbarElement("EDive/Parrel Sync", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 10)]
        public static MainToolbarElement CreateToolbarButton()
        {
            return MainToolbarElementFactory.Create(() =>
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
#else
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
                    OdinEditorWindow.InspectObjectInDropDown(dropdown, new Vector2(0, 20), 420);
                }
            }

            GUILayout.Space(2);
        }
#endif
        
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
