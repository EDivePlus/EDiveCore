// Author: František Holubec
// Created: 13.04.2025

using EDIVE.AddressableAssets;

namespace EDIVE.SceneManagement
{
    public interface IAddressableSceneDefinition : ISceneDefinition
    {
        public SceneAssetReference SceneReference { get; }
    }
}
