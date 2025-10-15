// Author: František Holubec
// Created: 15.10.2025

#if UNITY_EDITOR
using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.ApplicationConfigs.Components
{
    [Serializable]
    public class CloudProjectSettingsSnapshot : AApplicationConfigComponent
    {
        [Required]
        [SerializeField]
        private string _ProjectGuid;

        [Required]
        [SerializeField]
        private string _ProjectName;

        [Required]
        [SerializeField]
        private string _OrganizationId;

        private const string PROJECT_SETTINGS_ASSET_PATH = "ProjectSettings/ProjectSettings.asset";
        
        public override IEnumerator Apply()
        {
            if (Application.isBatchMode)
            {
                Debug.Log($"Setting cloud project to {_ProjectGuid}: {_ProjectName} ({_OrganizationId})");
                var projectSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(PROJECT_SETTINGS_ASSET_PATH)[0]);  
                var projectIDProperty = projectSettings.FindProperty ("cloudProjectId");
                var projectNameProperty = projectSettings.FindProperty ("projectName");
                var organizationIdProperty = projectSettings.FindProperty ("organizationId");
        
                projectIDProperty.stringValue = _ProjectGuid;
                projectNameProperty.stringValue = _ProjectName;
                organizationIdProperty.stringValue = _OrganizationId;
                projectSettings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            else
            {
                UnityConnectHelper.BindProject(_ProjectGuid, _ProjectName, _OrganizationId);
            }
            
            yield return null;
        }

        public override IEnumerator LoadCurrent()
        {
            _OrganizationId = CloudProjectSettings.organizationId;
            _ProjectGuid = CloudProjectSettings.projectId;
            _ProjectName = CloudProjectSettings.projectName;
            yield break;
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(_ProjectGuid) && 
                   !string.IsNullOrEmpty(_ProjectName) && 
                   !string.IsNullOrEmpty(_OrganizationId);
        }
    }
}
#endif