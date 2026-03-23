// Author: František Holubec
// Created: 20.03.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Utils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool
{
    [Serializable]
    public class BuildPreset
    {
        [HideInInspector]
        [SerializeField]
        private BuildUserConfig _UserConfig;
        
        [SerializeField]
        private BuildPlatformConfig _PlatformConfig;

        public BuildUserConfig UserConfig => _UserConfig;
        public BuildPlatformConfig PlatformConfig => _PlatformConfig;

        public BuildPreset() { }
        public BuildPreset(BuildUserConfig userConfig, BuildPlatformConfig platformConfig)
        {
            _UserConfig = userConfig;
            _PlatformConfig = platformConfig;
        }

        public void Build(BuildOptions options)
        {
            var buildRunner = new BuildRunner(this, options);
            buildRunner.StartBuild();
        }

        [EnhancedTableColumn(200)]
        [VerticalGroup("Build")]
        [HorizontalGroup("Build/Main")]
        [Button]
        public void Build() => Build(BuildUtils.BUILD_OPTIONS);

        [HorizontalGroup("Build/Main")]
        [Button("Build & Run")]
        public void BuildAndRun() => Build(BuildUtils.BUILD_AND_RUN_OPTIONS);

        public virtual void Validate()
        {
            if (UserConfig == null)
                throw new ArgumentNullException(nameof(UserConfig));
            
            if (PlatformConfig == null)
                throw new ArgumentNullException(nameof(PlatformConfig));
        }

        public IEnumerable<IBuildCallback> GetBuildCallbacks(BuildContext context) => 
            GetBuildCallbacks(context, BuildGlobalSettings.Instance, PlatformConfig, UserConfig);

        public IEnumerable<string> GetBuildDefines(BuildContext context) => 
            GetBuildDefines(context, BuildGlobalSettings.Instance, PlatformConfig, UserConfig);

        protected IEnumerable<IBuildCallback> GetBuildCallbacks(BuildContext context, params IBuildDataProvider[] providers) => 
            providers.Where(p => p != null).SelectMany(p => p.GetBuildCallbacks(context));

        protected IEnumerable<string> GetBuildDefines(BuildContext context, params IBuildDataProvider[] providers) => 
            providers.Where(p => p != null).SelectMany(p => p.GetBuildDefines(context));

        public override string ToString()
        {
            return $"User: '{(UserConfig != null ? UserConfig.name : "null")}' Platform: '{(PlatformConfig != null ? PlatformConfig.name : "null")}'";
        }
    }
}
