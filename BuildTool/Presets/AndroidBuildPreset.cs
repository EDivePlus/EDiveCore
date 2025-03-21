// Author: František Holubec
// Created: 20.03.2025

using System;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Runners;
using UnityEditor;

namespace EDIVE.BuildTool.Presets
{
    [Serializable]
    public class AndroidBuildPreset : ABuildPreset<AndroidBuildPlatformConfig>
    {
        public AndroidBuildPreset() { }
        public AndroidBuildPreset(BuildUserConfig userConfig, AndroidBuildPlatformConfig platformConfig) : base(userConfig, platformConfig) { }

        protected override ABuildRunner CreateBuildRunner(BuildOptions options) => new AndroidBuildRunner(this, options);
    }
}
