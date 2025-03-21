// Author: František Holubec
// Created: 20.03.2025

using System;
using EDIVE.BuildTool.PlatformConfigs;

namespace EDIVE.BuildTool.Presets
{
    [Serializable]
    public class StandaloneBuildPreset : ABuildPreset<StandaloneBuildPlatformConfig>
    {
        public StandaloneBuildPreset(BuildUserConfig userConfig, StandaloneBuildPlatformConfig platformConfig) : base(userConfig, platformConfig) { }
    }
}
