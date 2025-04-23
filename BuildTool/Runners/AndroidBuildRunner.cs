using System;
using System.Collections;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Presets;
using EDIVE.BuildTool.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.CrashReporting;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
using Unity.Android.Types;
using UnityEditor.Android;
using AndroidArchitecture = UnityEditor.AndroidArchitecture;
using AndroidBuildSystem = UnityEditor.AndroidBuildSystem;
#endif

namespace EDIVE.BuildTool.Runners
{
    [Serializable]
    public class AndroidBuildRunner : ABuildRunner<AndroidBuildPreset, AndroidBuildPlatformConfig>
    {
        [SerializeField]
        private AndroidBuildSystem _PrevSystem;

        [SerializeField]
        private ScriptingImplementation _PrevBackend;

        [SerializeField]
        private Il2CppCompilerConfiguration _PrevIl2CppConfig;

        [SerializeField]
        private Il2CppCodeGeneration _PrevIl2CppCodeGeneration;

        [SerializeField]
        private AndroidArchitecture _PrevArchitectures;

        [SerializeField]
        private bool _PrevBuildAppBundle;

        [SerializeField]
        private bool _PrevSplitAppBinary;

        [SerializeField]
        private bool _PrevMinifyDebug;

        [SerializeField]
        private bool _PrevMinifyRelease;

        [SerializeField]
        private bool _PrevEnableCloudDiagnostics;

#if UNITY_6000_0_OR_NEWER
        [SerializeField]
        private DebugSymbolFormat _PrevSymbolFormat;

        [SerializeField]
        private DebugSymbolLevel _PrevSymbolLevel;
#else
        [SerializeField]
        private AndroidCreateSymbols _PrevCreateSymbols;
#endif

        public AndroidBuildRunner() { }
        public AndroidBuildRunner(AndroidBuildPreset buildPreset, BuildOptions options = BuildOptions.None) : base(buildPreset, options) { }

        protected override void SetupSettingsBeforeBuild()
        {
            base.SetupSettingsBeforeBuild();

            _PrevSystem = EditorUserBuildSettings.androidBuildSystem;
            EditorUserBuildSettings.androidBuildSystem = PlatformConfig.BuildSystem;

            _PrevBackend = PlayerSettings.GetScriptingBackend(PlatformConfig.NamedBuildTarget);
            PlayerSettings.SetScriptingBackend(PlatformConfig.NamedBuildTarget, PlatformConfig.ScriptingImplementation);

            _PrevIl2CppConfig = PlayerSettings.GetIl2CppCompilerConfiguration(PlatformConfig.NamedBuildTarget);
            PlayerSettings.SetIl2CppCompilerConfiguration(PlatformConfig.NamedBuildTarget, PlatformConfig.Il2CppConfig);

            _PrevIl2CppCodeGeneration = PlayerSettings.GetIl2CppCodeGeneration(PlatformConfig.NamedBuildTarget);
            PlayerSettings.SetIl2CppCodeGeneration(PlatformConfig.NamedBuildTarget, PlatformConfig.Il2CppCodeGeneration);

            _PrevArchitectures = PlayerSettings.Android.targetArchitectures;
            PlayerSettings.Android.targetArchitectures = PlatformConfig.TargetArchitectures;

            _PrevBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            EditorUserBuildSettings.buildAppBundle = PlatformConfig.BuildAndroidAppBundle;

            _PrevMinifyDebug = PlayerSettings.Android.minifyDebug;
            PlayerSettings.Android.minifyDebug = PlatformConfig.MinifyDebug;

            _PrevMinifyRelease = PlayerSettings.Android.minifyRelease;
            PlayerSettings.Android.minifyRelease = PlatformConfig.MinifyRelease;

#if UNITY_6000_0_OR_NEWER
            _PrevSplitAppBinary = PlayerSettings.Android.splitApplicationBinary;
            PlayerSettings.Android.splitApplicationBinary = PlatformConfig.SplitApplicationBinary;

            _PrevSymbolLevel = UserBuildSettings.DebugSymbols.level;
            UserBuildSettings.DebugSymbols.level = PlatformConfig.SymbolLevel;

            _PrevSymbolFormat = UserBuildSettings.DebugSymbols.format;
            UserBuildSettings.DebugSymbols.format = PlatformConfig.SymbolFormat;
#else
            _PrevSplitAppBinary = PlayerSettings.Android.useAPKExpansionFiles;
            PlayerSettings.Android.useAPKExpansionFiles = PlatformConfig.SplitApplicationBinary;

            _PrevCreateSymbols = EditorUserBuildSettings.androidCreateSymbols;
            EditorUserBuildSettings.androidCreateSymbols = PlatformConfig.CreateSymbolsFile;
#endif

            _PrevEnableCloudDiagnostics = CrashReportingSettings.enabled;
            if (PlatformConfig.ForceDisableCloudDiagnostics) CrashReportingSettings.enabled = false;

            // TODO keystore if needed in the future
        }

        protected override void RestoreSettingsAfterBuild()
        {
            base.RestoreSettingsAfterBuild();
            PlayerSettings.SetScriptingBackend(PlatformConfig.NamedBuildTarget, _PrevBackend);
            PlayerSettings.SetIl2CppCompilerConfiguration(PlatformConfig.NamedBuildTarget, _PrevIl2CppConfig);
            PlayerSettings.SetIl2CppCodeGeneration(PlatformConfig.NamedBuildTarget, _PrevIl2CppCodeGeneration);

            EditorUserBuildSettings.androidBuildSystem = _PrevSystem;
            PlayerSettings.Android.targetArchitectures = _PrevArchitectures;

            EditorUserBuildSettings.buildAppBundle = _PrevBuildAppBundle;
            PlayerSettings.Android.minifyDebug = _PrevMinifyDebug;
            PlayerSettings.Android.minifyRelease = _PrevMinifyRelease;

#if UNITY_6000_0_OR_NEWER
            PlayerSettings.Android.splitApplicationBinary = _PrevSplitAppBinary;
            UserBuildSettings.DebugSymbols.level = _PrevSymbolLevel;
            UserBuildSettings.DebugSymbols.format = _PrevSymbolFormat;
#else
            PlayerSettings.Android.useAPKExpansionFiles = _PrevSplitAppBinary;
            EditorUserBuildSettings.androidCreateSymbols = _PrevCreateSymbols;
#endif

            CrashReportingSettings.enabled = _PrevEnableCloudDiagnostics;
        }

        protected override IEnumerator ProcessSuccessfulBuild()
        {
            if (PlatformConfig.BuildAndroidAppBundle && PlatformConfig.ExtractAppBundleApk)
            {
                BuildUtils.ExtractAAB(UserConfig.PathResolver.FullPath);
            }
            yield break;
        }
    }
}
