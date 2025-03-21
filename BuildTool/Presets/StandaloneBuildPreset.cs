// Author: František Holubec
// Created: 20.03.2025

using System;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Runners;
using UnityEditor;

namespace EDIVE.BuildTool.Presets
{
    [Serializable]
    public class StandaloneBuildPreset : ABuildPreset<StandaloneBuildPlatformConfig>
    {
        public StandaloneBuildPreset() { }
        public StandaloneBuildPreset(BuildUserConfig userConfig, StandaloneBuildPlatformConfig platformConfig) : base(userConfig, platformConfig) { }

        protected override ABuildRunner CreateBuildRunner(BuildOptions options) => new StandaloneBuildRunner(this, options);

    }
}
