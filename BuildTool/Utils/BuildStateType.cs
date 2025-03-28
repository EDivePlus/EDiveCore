// Author: František Holubec
// Created: 17.03.2025

namespace EDIVE.BuildTool.Utils
{
    public enum BuildStateType
    {
        NotStarted = 0,
        PreprocessPreDefines = 1,
        PreprocessPostDefines = 2,
        PipelinePreparation = 3,
        PipelineInProgress = 4,
        PipelineFinalization = 5,
        PostprocessPreDefines = 6,
        PostprocessPostDefines = 7,
        Completed = 8,
    }
}
