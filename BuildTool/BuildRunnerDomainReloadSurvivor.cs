using System;
using EDIVE.EditorUtils.DomainReload;
using UnityEngine;

namespace EDIVE.BuildTool
{
    [Serializable]
    public class BuildRunnerDomainReloadSurvivor : IDomainReloadSurvivor
    {
        [SerializeField]
        private BuildRunner _Runner;

        public BuildRunnerDomainReloadSurvivor(BuildRunner runner)
        {
            _Runner = runner;
        }

        public void OnAfterDomainReload()
        {
            DebugLite.Log("[BuildRunner] Survived Domain Reload...");
            _Runner.StartBuild();
        }
    }
}
