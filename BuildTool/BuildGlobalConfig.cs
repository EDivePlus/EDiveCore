// Author: František Holubec
// Created: 17.03.2025

using System.Collections.Generic;
using EDIVE.BuildTool.Utils;
using EDIVE.Core.Versions;
using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool
{
    [GlobalConfig("Assets/_Project/Settings/Editor/")]
    public class BuildGlobalConfig : GlobalConfig<BuildGlobalConfig>
    {
        [SerializeField]
        private AppVersionDefinition _VersionDefinition;

        [SerializeField]
        private BuildUserConfig _DefaultUser;

        [SerializeField]
        [InlineProperty]
        [HideLabel]
        private MultiPlatformBuildSetupData _BuildSetupData;

        private readonly AssetPersistentContext<BuildUserConfig> _currentUserContext = new (PersistentContext.Get("BuildGlobalConfig", "CurrentUser"));
        private BuildUserConfig CurrentUser
        {
            get => _currentUserContext.Value ??= _DefaultUser;
            set => _currentUserContext.Value = value;
        }

        public IEnumerable<BuildSetupData> GetBuildSetupData(NamedBuildTarget namedTarget, BuildTarget target) => _BuildSetupData.GetData(namedTarget, target);

        private const string SETTINGS_PATH = "Project/Build Config";

        [SettingsProvider]
        private static SettingsProvider RegisterSettingsProvider()
        {
            return Instance == null ? null : AssetSettingsProvider.CreateProviderFromObject(
                SETTINGS_PATH, Instance, new[] {"Build"});
        }
    }
}
