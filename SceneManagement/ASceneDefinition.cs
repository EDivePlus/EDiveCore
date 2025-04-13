// Author: František Holubec
// Created: 08.04.2025

using EDIVE.Utils.UniqueDefinitions;

namespace EDIVE.SceneManagement
{
    public abstract class ASceneDefinition : AUniqueDefinition, ISceneDefinition
    {
        public abstract bool IsValid();
    }
}
