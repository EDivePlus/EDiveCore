// Author: František Holubec
// Created: 21.07.2025

namespace EDIVE.Replay
{
    public enum ReplaySpawnMode
    {
        FindOrCreate = 0,   // Look for existing object in the scene, if not found, create new instance
        FindOnly = 1,       // Only look for existing object
        AlwaysCreate = 2    // Always create new instance
    }
}
