// Author: František Holubec
// Created: 15.10.2025

using System.Collections.Generic;
using EDIVE.BuildTool.BuildSetupData;
using EDIVE.BuildTool.Utils;
using UnityEditor;
using UnityEditor.Build;

namespace EDIVE.BuildTool
{
    public interface IBuildSetupDataProvider
    {
        IEnumerable<IBuildSetupData> GetBuildSetupData(NamedBuildTarget namedTarget, BuildTarget target);
    }
}
