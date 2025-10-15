// Author: František Holubec
// Created: 20.03.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.Actions;
using EDIVE.BuildTool.BuildSetupData;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Utils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
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
        private ABuildPlatformConfig _PlatformConfig;

        public BuildUserConfig UserConfig => _UserConfig;
        public ABuildPlatformConfig PlatformConfig => _PlatformConfig;

        public BuildPreset() { }
        public BuildPreset(BuildUserConfig userConfig, ABuildPlatformConfig platformConfig)
        {
            _UserConfig = userConfig;
            _PlatformConfig = platformConfig;
        }

        public void Build(BuildOptions options)
        {
            var buildRunner = _PlatformConfig.CreateBuildRunner(this, options);
            if (buildRunner == null)
            {
                Debug.LogError("Invalid Build runner!");
                return;
            }
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

        public IEnumerable<IBuildAction> GetBuildActions(NamedBuildTarget namedTarget, BuildTarget target)
        {
            return GetSetupData(namedTarget, target)
                .SelectMany(d => d.Actions)
                .Where(a => a != null)
                .OrderBy(a => a.Priority);
        }

        public IEnumerable<string> GetDefines(NamedBuildTarget namedTarget, BuildTarget target)
        {
            return GetSetupData(namedTarget, target)
                .SelectMany(d => d.Defines)
                .Where(d => !string.IsNullOrEmpty(d));
        }

        protected IEnumerable<IBuildSetupData> GetSetupData(NamedBuildTarget namedTarget, BuildTarget target)
        {
            return GetSetupData(namedTarget, target, BuildGlobalSettings.Instance, PlatformConfig, UserConfig);
        }
        
        protected IEnumerable<IBuildSetupData> GetSetupData(NamedBuildTarget namedTarget, BuildTarget target, params IBuildSetupDataProvider[] providers)
        {
            return providers.Where(provider => provider != null)
                .SelectMany(provider => provider.GetBuildSetupData(namedTarget, target));
        }
        
        public override string ToString()
        {
            return $"User: '{(UserConfig != null ? UserConfig.name : "null")}' Platform: '{(PlatformConfig != null ? PlatformConfig.name : "null")}'";
        }
    }
}
