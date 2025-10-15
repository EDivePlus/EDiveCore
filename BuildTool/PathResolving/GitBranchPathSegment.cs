// Author: František Holubec
// Created: 21.03.2025

using System;
using EDIVE.BuildTool.Utils;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class GitBranchPathSegment : ABuildPathSegment
    {
        public override string GetValue(BuildPreset preset) => BuildUtils.TryGetCurrentGitBranch(out var branch) ? branch : "UnknownBranch";
    }
}
