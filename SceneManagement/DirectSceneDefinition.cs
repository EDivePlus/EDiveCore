// Author: František Holubec
// Created: 11.04.2025

using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.SceneManagement
{
    public class DirectSceneDefinition : ASceneDefinition
    {
        [SceneReference(SceneReferenceType.Path, true)]
        [SerializeField]
        private string _SceneAsset;
        
        public string SceneAsset => _SceneAsset;
        
        public override bool IsValid() => SceneUtility.GetBuildIndexByScenePath(_SceneAsset) >= 0;
        public override ASceneInstance CreateInstance() => new DirectSceneInstance(this);
    }
}
