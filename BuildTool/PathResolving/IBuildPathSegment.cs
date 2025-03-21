// Author: František Holubec
// Created: 21.03.2025

using System;
using EDIVE.BuildTool.Presets;
using EDIVE.BuildTool.Utils;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.PathResolving
{
    public abstract class ABuildPathSegment
    {
        public abstract string GetValue(ABuildPreset preset);
    }

    [Serializable]
    public class StringPathSegment : ABuildPathSegment
    {
        [SerializeField]
        public string _Value;
        public override string GetValue(ABuildPreset preset) => _Value;
    }

    [Serializable]
    public class ProductNamePathSegment : ABuildPathSegment
    {
        public override string GetValue(ABuildPreset preset) => PlayerSettings.productName;
    }

    [Serializable]
    public class PlatformNamePathSegment : ABuildPathSegment
    {
        public override string GetValue(ABuildPreset preset) => preset.BasePlatformConfig.PlatformName;
    }

    [Serializable]
    public class PlatformConfigIDPathSegment : ABuildPathSegment
    {
        public override string GetValue(ABuildPreset preset) => preset.BasePlatformConfig.ConfigID;
    }

    [Serializable]
    public class GitBranchPathSegment : ABuildPathSegment
    {
        public override string GetValue(ABuildPreset preset) => BuildUtils.TryGetCurrentGitBranch(out var branch) ? branch : "UnknownBranch";
    }
}
