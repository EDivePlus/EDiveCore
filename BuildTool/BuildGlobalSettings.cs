// Author: František Holubec
// Created: 17.03.2025

using System.Collections.Generic;
using EDIVE.BuildTool.Presets;
using EDIVE.BuildTool.Utils;
using EDIVE.Core.Versions;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
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

        [PropertyOrder(10)]
        [PropertySpace]
        [EnhancedTableList]
        [HideReferenceObjectPicker]
        [SerializeReference]
        private List<ABuildPreset> _Presets;

        [PropertyOrder(100)]
        [PropertySpace]
        [InlineProperty]
        [HideLabel]
        [SerializeField]
        private MultiPlatformBuildSetupData _BuildSetupData;

        public AppVersionDefinition VersionDefinition => _VersionDefinition;
        public BuildUserConfig DefaultUser => _DefaultUser;

        [PropertySpace]
        [ShowInInspector]
        public BuildUserConfig CurrentUser
        {
            get => BuildGlobalUserSettings.instance.CurrentUser ?? DefaultUser;
            set => BuildGlobalUserSettings.instance.CurrentUser = value;
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
