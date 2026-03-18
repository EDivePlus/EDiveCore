// Author: František Holubec
// Created: 21.03.2025

using System;
using EDIVE.BuildTool.PlatformConfigs;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class PlatformNamePathSegment : ABuildPathSegment
    {
        public override string GetValue(BuildPreset preset) => preset.PlatformConfig.TryGetModule<PathPlatformModule>(out var module) ? module.PlatformName : "UnknownPlatform";
    }
}
