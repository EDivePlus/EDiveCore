// Author: František Holubec
// Created: 20.03.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.BuildSetupData;
using EDIVE.BuildTool.PathResolving;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.BuildTool
{
    public class BuildUserConfig : ScriptableObject, IBuildDataProvider
    {
        [BoxGroup("Path Resolver")]
        [InlineProperty]
        [HideLabel]
        [SerializeField]
        private BuildPathResolver _PathResolver;

        [PropertySpace(4)] 
        [SerializeField]
        [InlineProperty]
        [HideLabel]
        private MultiPlatformBuildSetupData _BuildSetupData;

        public BuildPathResolver PathResolver => _PathResolver;
        
        public IEnumerable<string> GetBuildDefines(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget)
                .SelectMany(d => d.Defines);
        }

        public IEnumerable<string> GetBuildScenes(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget)
                .SelectMany(d => d.Scenes);
        }
        
        public IEnumerable<IBuildCallback> GetBuildCallbacks(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget)
                .SelectMany(d => d.Actions);
        }
    }
}
