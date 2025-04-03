// Author: František Holubec
// Created: 17.03.2025

using System.Collections.Generic;
using EDIVE.BuildTool.Utils;
using EDIVE.Core.Versions;
using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool
{
    [GlobalConfig("Assets/_Project/Settings/Editor/")]
    public class BuildGlobalSettings : ProjectSettingsGlobalConfig<BuildGlobalSettings>
    {
        [SerializeField]
        private AppVersionDefinition _VersionDefinition;

        [SerializeField]
        private BuildUserConfig _DefaultUser;

        [PropertyOrder(100)]
        [PropertySpace]
        [InlineProperty]
        [HideLabel]
        [SerializeField]
        private MultiPlatformBuildSetupData _BuildSetupData;

        public AppVersionDefinition VersionDefinition => _VersionDefinition;
        public BuildUserConfig DefaultUser => _DefaultUser;

        private AssetPersistentContext<BuildUserConfig> _defaultUserContext;
        public BuildUserConfig CurrentUser
        {
            get => _defaultUserContext.Value != null ? _defaultUserContext.Value : DefaultUser;
            set => _defaultUserContext.Value = value;
        }

        public IEnumerable<BuildSetupData> GetBuildSetupData(NamedBuildTarget namedTarget, BuildTarget target) => _BuildSetupData.GetData(namedTarget, target);

        private const string SETTINGS_PATH = "Project/Build Config";

        [SettingsProvider]
        private static SettingsProvider RegisterSettingsProvider()
        {
            return Instance == null ? null : AssetSettingsProvider.CreateProviderFromObject(
                SETTINGS_PATH, Instance, new[] {"Build"});
        }

        [OnInspectorInit]
        private void OnInspectorInit()
        {
            _defaultUserContext = new AssetPersistentContext<BuildUserConfig>(PersistentContext.Get("BuildTool", "CurrentUser", ""));
        }
    }
}
