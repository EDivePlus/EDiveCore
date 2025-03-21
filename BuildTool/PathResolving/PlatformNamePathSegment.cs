// Author: František Holubec
// Created: 21.03.2025

using System;
using EDIVE.BuildTool.Presets;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class PlatformNamePathSegment : ABuildPathSegment
    {
        public override string GetValue(ABuildPreset preset) => preset.BasePlatformConfig.PlatformName;
    }
}
