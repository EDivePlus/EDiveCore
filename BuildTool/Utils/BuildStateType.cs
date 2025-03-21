// Author: František Holubec
// Created: 17.03.2025

namespace EDIVE.BuildTool.Utils
{
    public enum BuildStateType
    {
        NotStarted = 0,
        Preprocessing = 1,
        PipelinePreparation = 2,
        PipelineInProgress = 3,
        PipelineFinalization = 4,
        Postprocessing = 5,
        Completed = 6,
    }
}
