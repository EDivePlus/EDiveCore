// Author: František Holubec
// Created: 08.04.2025

using EDIVE.Addressables;
using EDIVE.MirrorNetworking.Scenes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.MirrorNetworking
{
    public class AddressableSceneDefinition : ASceneDefinition
    {
        [SerializeField]
        [Required]
        private SceneAssetReference _Scene;
    }

    public enum SceneLoadType
    {
        UnloadOnExit,
        Preload,
        StoreOnFirstLoad
    }
}
