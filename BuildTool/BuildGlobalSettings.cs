// Author: František Holubec
// Created: 17.03.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.ApplicationConfigs;
using EDIVE.BuildTool.BuildSetupData;
using EDIVE.Core.Versions;
using EDIVE.DataStructures;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool
{
    [GlobalConfig("Assets/_Shared/Settings/Editor/")]
    public class BuildGlobalSettings : GlobalConfig<BuildGlobalSettings>, IBuildDataProvider
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
        
        private GlobalPersistentContext<string> _currentAppContext;
        private GlobalPersistentContext<string> CurrentAppContext => _currentAppContext ??= PersistentContext.Get("BuildTool", "CurrentApp", "");

        public BuildUserConfig CurrentUser
        {
            get => !string.IsNullOrEmpty(CurrentUserContext.Value) ? AssetDatabase.LoadAssetAtPath<BuildUserConfig>(AssetDatabase.GUIDToAssetPath(CurrentUserContext.Value)) : DefaultUser;
            set => CurrentUserContext.Value = value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : null;
        }
        
        public ApplicationConfig CurrentApp
        {
            get => !string.IsNullOrEmpty(CurrentAppContext.Value) ? AssetDatabase.LoadAssetAtPath<ApplicationConfig>(AssetDatabase.GUIDToAssetPath(CurrentAppContext.Value)) : null;
            set => CurrentAppContext.Value = value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : null;
        }

        private const string SETTINGS_PATH = "Project/Build Config";

        [SettingsProvider]
        private static SettingsProvider RegisterSettingsProvider()
        {
            return Instance == null ? null : AssetSettingsProvider.CreateProviderFromObject(
                SETTINGS_PATH, Instance, new[] {"Build"});
        }

        public IEnumerable<string> GetBuildDefines(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget).SelectMany(d => d.Defines);
        }
        
        public IEnumerable<string> GetBuildScenes(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget).SelectMany(d => d.Scenes);
        }
        
        public IEnumerable<IBuildCallback> GetBuildCallbacks(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget).SelectMany(d => d.Actions);
        }
    }
}
