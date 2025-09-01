// Author: František Holubec
// Created: 11.04.2025

using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.SceneManagement
{
    public class DirectSceneDefinition : ASceneDefinition
    {
        [EnhancedValidate("ValidateSceneAsset")]
        [SceneReference(SceneReferenceType.Path)]
        [SerializeField]
        private string _SceneAsset;
        
        public string SceneAsset => _SceneAsset;
        
        public override bool IsValid() => SceneUtility.GetBuildIndexByScenePath(_SceneAsset) >= 0;
        public override ASceneInstance CreateInstance() => new DirectSceneInstance(this);
        
#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateSceneAsset(SelfValidationResult result)
        {
            if (!IsValid())
            {
                result.AddError($"Scene {_SceneAsset} is not in build settings!")
                    .WithFix(() =>
                    {
                        EditorBuildSettings.globalScenes = EditorBuildSettings.globalScenes.Append(new EditorBuildSettingsScene(_SceneAsset, true)).ToArray();
                    });
            }
        }
#endif
    }
}
