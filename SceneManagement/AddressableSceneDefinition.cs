// Author: František Holubec
// Created: 08.04.2025

#if ADDRESSABLES
using EDIVE.AddressableAssets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.SceneManagement
{
    public class AddressableSceneDefinition : ASceneDefinition
    {
        [SerializeField]
        [Required]
        private SceneAssetReference _SceneReference;
        public SceneAssetReference SceneReference => _SceneReference;
        
        public override bool IsValid() => SceneReference.IsValid();
        public override ASceneInstance CreateInstance() => new AddressableSceneInstance(this);
    }
}
#endif
