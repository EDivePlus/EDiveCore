// Author: František Holubec
// Created: 11.04.2025

namespace EDIVE.SceneManagement
{
    public interface ISceneDefinition 
    {
        string UniqueID { get; }
        bool IsValid();
    }
}
