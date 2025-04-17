// Author: František Holubec
// Created: 08.04.2025

using EDIVE.AssetTranslation;

namespace EDIVE.SceneManagement
{
    public abstract class ASceneDefinition : AUniqueDefinition
    {
        public abstract bool IsValid();
        public abstract ASceneInstance CreateInstance();
    }
}
