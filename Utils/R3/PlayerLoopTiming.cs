// Author: František Holubec
// Created: 08.07.2025

#if R3
namespace EDIVE.Utils.R3
{
    public enum PlayerLoopTiming
    {
        Initialization = 0,
        EarlyUpdate = 1,
        FixedUpdate = 2,
        PreUpdate = 3,
        Update = 4,
        PreLateUpdate = 5,
        PostLateUpdate = 6,
        TimeUpdate = 7,
    }
}
#endif
