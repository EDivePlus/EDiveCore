// Author: František Holubec
// Created: 11.04.2025

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.SceneManagement
{
    public class DirectSceneDefinition : ASceneDefinition
    {
        [ValidateInput(nameof(IsValid), "Scene not in build settings!", InfoMessageType.Warning)]
        [SceneReference(SceneReferenceType.Path)]
        [SerializeField]
        private string _SceneAsset;
        
        public string SceneAsset => _SceneAsset;
        
        public override bool IsValid() => SceneUtility.GetBuildIndexByScenePath(_SceneAsset) >= 0;
        public override ASceneInstance CreateInstance() => new DirectSceneInstance(this);
    }
}
