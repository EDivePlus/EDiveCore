#if PARREL_SYNC
using System.Collections.Generic;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using ParrelSync;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.External.ParrelSync
{
    public class EnhancedClonesManagerWindow : OdinEditorWindow
    {
        [HideIf(nameof(IsClone))]
        [ShowInInspector]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(DraggableItems = false, CustomAddFunction = nameof(CustomAddCloneFunction), CustomRemoveElementFunction = nameof(CustomRemoveCloneFunction), OnTitleBarGUI = nameof(OnClonesTitleBarGUI))]
        public List<ProjectCloneRecord> CloneRecords
        {
            get => ParrelSyncUtility.CloneRecords;
            set { }
        }

        [ShowIf(nameof(IsClone))]
        [InfoBox("Original project seems lost. You have to manually open the original and create a new clone instead of this one.", InfoMessageType.Error, nameof(IsOriginalProjectInvalid))]
        [ShowOpenInExplorer]
        [ShowInInspector]
        private string MasterProjectPath
        {
            get => ClonesManager.GetOriginalProjectPath();
            set { }
        }

        [ShowIf(nameof(IsClone))]
        [ShowInInspector]
        [HideLabel]
        [InlineProperty]
        [BoxGroup("Clone Arguments")]
        [HideReferenceObjectPicker]
        private ParrelSyncArgumentsBundle ArgumentsBundle
        {
            get => ParrelSyncUtility.SelfArgumentsBundle;
            set => ParrelSyncUtility.SelfArgumentsBundle = value;
        }

        private bool IsOriginalProjectInvalid => MasterProjectPath.IsNullOrWhitespace();
        private bool IsClone => ClonesManager.IsClone();

        [MenuItem("ParrelSync/Enhanced Clones Manager", priority = -100)]
        private static void InitWindow()
        {
            var window = GetWindow<EnhancedClonesManagerWindow>();
            window.titleContent = new GUIContent("Clones Manager");
            window.Show();
        }

        protected override void Initialize()
        {
            base.Initialize();
            ParrelSyncUtility.RefreshClones();
        }

        [PropertyOrder(-100)]
        [OnInspectorGUI]
        protected virtual void DrawTitle()
        {
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            var titleStyle = new GUIStyle(SirenixGUIStyles.BoldTitle) {fontSize = 25};
            var titleText = IsClone
                ? GUIHelper.TempContent(" Clone Project", FontAwesomeEditorIcons.CloneSolid.Highlighted)
                : GUIHelper.TempContent(" Master Project", FontAwesomeEditorIcons.CrownSolid.Highlighted);
            EditorGUILayout.LabelField(titleText, titleStyle, GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        private void CustomAddCloneFunction()
        {
            if (EditorUtility.DisplayDialog(
                    "Create new clone?",
                    $"Are you sure you want to create new clone of this project?",
                    "Create",
                    "Cancel"))
            {
                ParrelSyncUtility.CreateCloneFromCurrent();
            }
        }

        private void CustomRemoveCloneFunction(ProjectCloneRecord cloneRecord)
        {
            if (EditorUtility.DisplayDialog(
                    "Delete the clone?",
                    $"Are you sure you want to delete the clone project '{cloneRecord.ProjectPath}'?",
                    "Delete",
                    "Cancel"))
            {
                ParrelSyncUtility.DeleteClone(cloneRecord.ProjectPath);
            }
        }

        private void OnClonesTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                ParrelSyncUtility.RefreshClones();
            }
        }
    }
}
#endif
