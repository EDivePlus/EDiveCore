// Author: František Holubec
// Created: 20.03.2025

using System;
using EDIVE.BuildTool.PlatformConfigs;

namespace EDIVE.BuildTool.Presets
{
    [Serializable]
    public class AndroidBuildPreset : ABuildPreset<AndroidBuildPlatformConfig>
    {
        public AndroidBuildPreset(BuildUserConfig userConfig, AndroidBuildPlatformConfig platformConfig) : base(userConfig, platformConfig) { }
    }
}
