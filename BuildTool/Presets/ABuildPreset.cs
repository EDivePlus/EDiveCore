// Author: František Holubec
// Created: 20.03.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.Actions;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Runners;
using EDIVE.BuildTool.Utils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.Presets
{
    [Serializable]
    public abstract class ABuildPreset
    {
        [HideInInspector]
        [SerializeField]
        private BuildUserConfig _UserConfig;

        public BuildUserConfig UserConfig => _UserConfig;
        public abstract ABuildPlatformConfig BasePlatformConfig { get; }

        protected ABuildPreset() { }
        protected ABuildPreset(BuildUserConfig userConfig) { _UserConfig = userConfig; }

        public abstract void Build(BuildOptions options);

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
        }

        public IEnumerable<ABuildAction> GetBuildActions(NamedBuildTarget namedTarget, BuildTarget target)
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

        protected abstract IEnumerable<BuildSetupData> GetSetupData(NamedBuildTarget namedTarget, BuildTarget target);
    }

    [Serializable]
    public abstract class ABuildPreset<TPlatformConfig> : ABuildPreset where TPlatformConfig : ABuildPlatformConfig
    {
        [SerializeField]
        private TPlatformConfig _PlatformConfig;

        public TPlatformConfig PlatformConfig => _PlatformConfig;
        public override ABuildPlatformConfig BasePlatformConfig => _PlatformConfig;

        protected ABuildPreset() { }
        protected ABuildPreset(BuildUserConfig userConfig, TPlatformConfig platformConfig) : base(userConfig)
        {
            _PlatformConfig = platformConfig;
        }

        public override void Build(BuildOptions options)
        {
            var buildRunner = CreateBuildRunner(options);
            if (buildRunner == null)
            {
                Debug.LogError("Invalid Build runner!");
                return;
            }
            EditorCoroutineUtility.StartCoroutineOwnerless(buildRunner.StartBuild());
        }

        protected abstract ABuildRunner CreateBuildRunner(BuildOptions options);

        public override void Validate()
        {
            base.Validate();
            if (PlatformConfig == null)
                throw new ArgumentNullException(nameof(PlatformConfig));

            if (PlatformConfig.SceneList == null)
                throw new ArgumentNullException(nameof(PlatformConfig.SceneList));

            if (PlatformConfig.SceneList.Scenes.Count == 0)
                throw new ArgumentException("Scene list is empty", nameof(PlatformConfig.SceneList));
        }

        protected override IEnumerable<BuildSetupData> GetSetupData(NamedBuildTarget namedTarget, BuildTarget target)
        {
            foreach (var setupData in BuildGlobalSettings.Instance.GetBuildSetupData(namedTarget, target))
                yield return setupData;

            yield return PlatformConfig.BuildSetupData;

            foreach (var setupData in UserConfig.GetBuildSetupData(namedTarget, target))
                yield return setupData;
        }
    }
}
