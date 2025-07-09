using EDIVE.BuildTool.Presets;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.CrashReporting;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
using System;
using Unity.Android.Types;
using AndroidArchitecture = UnityEditor.AndroidArchitecture;
using AndroidBuildSystem = UnityEditor.AndroidBuildSystem;
#endif

namespace EDIVE.BuildTool.PlatformConfigs
{
    public class AndroidBuildPlatformConfig : ABuildPlatformConfig
    {
        [EnhancedBoxGroup("Backend", "@ColorTools.Purple", Order = -1)]
        [SerializeField]
        private AndroidArchitecture _TargetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        [EnhancedBoxGroup("Backend")]
        [SerializeField]
        private AndroidBuildSystem _BuildSystem = AndroidBuildSystem.Gradle;

        [PropertySpace(5)]
        [EnhancedBoxGroup("Backend")]
        [SerializeField]
        private ScriptingImplementation _ScriptingImplementation = ScriptingImplementation.IL2CPP;

        [EnhancedBoxGroup("Backend")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [LabelText("IL2CPP Config")]
        [SerializeField]
        private Il2CppCompilerConfiguration _Il2CppConfig = Il2CppCompilerConfiguration.Release;

        [EnhancedBoxGroup("Backend")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [PropertyTooltip("IL2CPP compiler will generate code optimized for:\nOptimizeSpeed - runtime performance.\nOptimizeSize - size and build time")]
        [LabelText("IL2CPP Code Generation")]
        [SerializeField]
        private Il2CppCodeGeneration _Il2CppCodeGeneration = Il2CppCodeGeneration.OptimizeSpeed;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _BuildAndroidAppBundle;

        [EnhancedBoxGroup("Build")]
        [EnableIf("BuildAndroidAppBundle")]
        [SerializeField]
        private bool _ExtractAppBundleApk = true;

        [PropertySpace(5)]
        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _MinifyDebug;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _MinifyRelease;

        [PropertySpace(5)]
        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _SplitApplicationBinary;

#if UNITY_6000_0_OR_NEWER
        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [InfoBox("Unity forces Full symbols when CloudDiagnostics is enabled", InfoMessageType.Warning, nameof(ShowForcedSymbolsMessage))]
        [SerializeField]
        private DebugSymbolLevel _SymbolLevel = DebugSymbolLevel.None;

        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [EnumToggleButtons]
        [SerializeField]
        private DebugSymbolFormatFlags _SymbolFormat = DebugSymbolFormatFlags.Zip | DebugSymbolFormatFlags.AppBundle;

        private bool ShowForcedSymbolsMessage => CrashReportingSettings.enabled && !_ForceDisableCloudDiagnostics && _SymbolLevel != DebugSymbolLevel.Full;
#else
        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [InfoBox("Unity forces Debugging symbols when CloudDiagnostics is enabled", InfoMessageType.Warning, nameof(ShowForcedSymbolsMessage))]
        [SerializeField]
        private AndroidCreateSymbols _CreateSymbolsFile;

        private bool ShowForcedSymbolsMessage => CrashReportingSettings.enabled && !_ForceDisableCloudDiagnostics && _CreateSymbolsFile != AndroidCreateSymbols.Debugging;
#endif

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _ForceDisableCloudDiagnostics;

        public bool BuildAndroidAppBundle => _BuildAndroidAppBundle;
        public bool ExtractAppBundleApk => _ExtractAppBundleApk;
        public AndroidArchitecture TargetArchitectures => _TargetArchitectures.SanitizeFlags();
        public AndroidBuildSystem BuildSystem => _BuildSystem;

        public bool MinifyDebug => _MinifyDebug;
        public bool MinifyRelease => _MinifyRelease;

        public ScriptingImplementation ScriptingImplementation => _ScriptingImplementation;
        public Il2CppCompilerConfiguration Il2CppConfig => _Il2CppConfig;
        public Il2CppCodeGeneration Il2CppCodeGeneration => _Il2CppCodeGeneration;
        public bool SplitApplicationBinary => _SplitApplicationBinary;

#if UNITY_6000_0_OR_NEWER
        public DebugSymbolFormat SymbolFormat => (DebugSymbolFormat)_SymbolFormat;
        public DebugSymbolLevel SymbolLevel => _SymbolLevel;
#else
        public AndroidCreateSymbols CreateSymbolsFile => _CreateSymbolsFile;
#endif

        public bool ForceDisableCloudDiagnostics => _ForceDisableCloudDiagnostics;
        public override NamedBuildTarget NamedBuildTarget => NamedBuildTarget.Android;
        public override BuildTarget BuildTarget => BuildTarget.Android;
        public override string BuildExtension => BuildAndroidAppBundle ? ".aab" : ".apk";

        public override ABuildPreset CreatePreset(BuildUserConfig userConfig) => new AndroidBuildPreset(userConfig, this);
    }

#if UNITY_6000_0_OR_NEWER
    [Flags]
    public enum DebugSymbolFormatFlags
    {
        [Tooltip("Create .zip file")] Zip = 1,
        [Tooltip("Include in AppBundle (Requires App Bundle)")] AppBundle = 2,
        [Tooltip("use .so.dbg instead of .so extension")] LegacyExtensions = 4,
    }
#endif
}
