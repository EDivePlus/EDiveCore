using System;
using EDIVE.EditorUtils.DomainReload;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace EDIVE.BuildTool.Runners
{
    [Serializable]
    public class BuildRunnerDomainReloadSurvivor : IDomainReloadSurvivor
    {
        [SerializeReference]
        private ABuildRunner _Runner;

        public BuildRunnerDomainReloadSurvivor(ABuildRunner runner)
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
