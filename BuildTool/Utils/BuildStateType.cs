// Author: František Holubec
// Created: 17.03.2025

namespace EDIVE.BuildTool.Utils
{
    public enum BuildStateType
    {
        NotStarted,             // Build not started
        StateCapture,           // Capturing current state of the editor
        BuildTargetSwitch,      // Switching to the desired build target
        Preprocess,             // Build target is prepared and defines applied
        PipelinePreparation,    // Pipeline is prepared and ready to run
        PipelineInProgress,     // Pipeline is running
        PipelineFinalization,   // Pipeline completed and finalizing
        Postprocess,            // Build target and defines are still in build state
        BuildTargetRevert,      // Reverting to the original build target, skipped in batch mode
        StateRestore,           // Restoring editor to the original state, skipped in batch mode
        Completed,              // Build completed
    }
}
