// Author: František Holubec
// Created: 17.03.2025

using System.Collections.Generic;
using EDIVE.BuildTool.Utils;
using EDIVE.Core.Versions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool
{
    [GlobalConfig("Assets/_Shared/Settings/Editor/")]
    public class BuildGlobalSettings : GlobalConfig<BuildGlobalSettings>
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

        private GlobalPersistentContext<string> _currentUserContext;
        private GlobalPersistentContext<string> CurrentUserContext => _currentUserContext ??= PersistentContext.Get("BuildTool", "CurrentUser", "");

        public BuildUserConfig CurrentUser
        {
            get => !string.IsNullOrEmpty(CurrentUserContext.Value) ? AssetDatabase.LoadAssetAtPath<BuildUserConfig>(AssetDatabase.GUIDToAssetPath(CurrentUserContext.Value)) : DefaultUser;
            set => CurrentUserContext.Value = value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : null;
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
