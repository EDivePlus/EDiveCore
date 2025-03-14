#if UNITY_EDITOR
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace EDIVE.AppLoading
{
    [GlobalConfig("Assets/_Project/Settings/")]
    public class LoaderSettings : GlobalConfig<LoaderSettings>
    {
        [SerializeField]
#if ADDRESSABLES
        [SceneReference(SceneReferenceType.Path)]
#else
        [SceneReference(SceneReferenceType.Path, true)]
#endif
        private string _RootScene;
        public static string RootScene { get => Instance._RootScene; set => Instance._RootScene = value; }

        [MenuItem("Tools/App Loader")]
        public static void OpenLoaderSettings()
        {
            SettingsService.OpenProjectSettings(SettingsPath);
        }

        private static string SettingsPath => "Project/App Loader";
        private static IEnumerable<string> Keywords => new[] {"Loader", "Loading"};

        [SettingsProvider]
        protected static SettingsProvider RegisterSettingsProvider()
        {
            return Instance == null ? null : AssetSettingsProvider.CreateProviderFromObject(SettingsPath, Instance, Keywords);
        }
    }
}
#endif
