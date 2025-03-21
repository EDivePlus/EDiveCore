// Author: František Holubec
// Created: 20.03.2025

using System.Collections.Generic;
using EDIVE.BuildTool.PathResolving;
using EDIVE.BuildTool.Utils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool
{
    public class BuildUserConfig : ScriptableObject
    {
        [SerializeField]
        private BuildPathResolver _PathResolver;

        [SerializeField]
        [InlineProperty]
        [HideLabel]
        private MultiPlatformBuildSetupData _BuildSetupData;

        public BuildPathResolver PathResolver => _PathResolver;
        public IEnumerable<BuildSetupData> GetBuildSetupData(NamedBuildTarget namedTarget, BuildTarget target) => _BuildSetupData.GetData(namedTarget, target);
    }
}
