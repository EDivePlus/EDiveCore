// Author: František Holubec
// Created: 17.03.2025

namespace EDIVE.BuildTool.Utils
{
    public enum BuildStateType
    {
        NotStarted = 0,             // Build not started
        StateCapture = 1,           // Capturing current state of the editor
        Preprocess  = 2,            // Build target is prepared and defines applied
        PipelinePreparation = 3,    // Pipeline is prepared and ready to run
        PipelineInProgress = 4,     // Pipeline is running
        PipelineFinalization = 5,   // Pipeline completed and finalizing
        Postprocess = 6,            // Build target and defines are still in build state
        StateRestore = 7,           // Restoring editor to the original state
        Completed = 8,              // Build completed
    }
}
