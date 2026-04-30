// Author: František Holubec
// Created: 18.03.2026

#if UNITY_ANDROID
using System;
using System.Collections;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Unity.Android.Types;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.CrashReporting;
using UnityEngine;
using AndroidArchitecture = UnityEditor.AndroidArchitecture;
using AndroidBuildSystem = UnityEditor.AndroidBuildSystem;

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public class AndroidBuildPlatformModule : ABuildTargetPlatformModule
    {
        public override string PlatformName => "Android";
        
        [EnhancedBoxGroup("Backend", "@ColorTools.Purple", Order = -1, SpaceAfter = 4)]
        [SerializeField]
        private AndroidArchitecture _TargetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        public AndroidArchitecture TargetArchitectures => _TargetArchitectures;

        [EnhancedBoxGroup("Backend")]
        [SerializeField]
        private AndroidBuildSystem _BuildSystem = AndroidBuildSystem.Gradle;
        public AndroidBuildSystem BuildSystem => _BuildSystem;
        
        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _BuildAndroidAppBundle;
        public bool BuildAndroidAppBundle => _BuildAndroidAppBundle;

        [PropertySpace(5)]
        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _MinifyDebug;
        public bool MinifyDebug => _MinifyDebug;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _MinifyRelease;
        public bool MinifyRelease => _MinifyRelease;

        [PropertySpace(5)]
        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _SplitApplicationBinary;
        public bool SplitApplicationBinary => _SplitApplicationBinary;
        
        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [InfoBox("Unity forces Full symbols when CloudDiagnostics is enabled", InfoMessageType.Warning, nameof(ShowForcedSymbolsMessage))]
        [SerializeField]
        private DebugSymbolLevel _SymbolLevel = DebugSymbolLevel.None;
        public DebugSymbolLevel SymbolLevel => _SymbolLevel;

        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [SerializeField]
        private DebugSymbolsOutputFormat _SymbolOutputFormat = DebugSymbolsOutputFormat.ZipAndIncludeInBundle;
        
        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [SerializeField]
        private DebugSymbolFileExtension _SymbolFileExtension = DebugSymbolFileExtension.Standard;

        public DebugSymbolFormat SymbolFormat => (DebugSymbolFormat)((int) _SymbolOutputFormat | (int) _SymbolFileExtension);

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _ForceDisableCloudDiagnostics;
        public bool ForceDisableCloudDiagnostics => _ForceDisableCloudDiagnostics;

        private bool ShowForcedSymbolsMessage => CrashReportingSettings.enabled && !_ForceDisableCloudDiagnostics && _SymbolLevel != DebugSymbolLevel.Full;
        
        public override NamedBuildTarget NamedBuildTarget => NamedBuildTarget.Android;
        public override BuildTarget BuildTarget => BuildTarget.Android;
        public override string BuildExtension => _BuildAndroidAppBundle ? ".aab" : ".apk";

        public override IEnumerator OnStateCapture(BuildContext context)
        {
            yield return base.OnStateCapture(context);
            var data = context.GetOrCreateData<Data>();
            
            data._PrevSystem = EditorUserBuildSettings.androidBuildSystem;
            EditorUserBuildSettings.androidBuildSystem = _BuildSystem;

            data._PrevArchitectures = PlayerSettings.Android.targetArchitectures;
            PlayerSettings.Android.targetArchitectures = _TargetArchitectures;

            data._PrevBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            EditorUserBuildSettings.buildAppBundle = _BuildAndroidAppBundle;
            
            data._PrevMinifyDebug = PlayerSettings.Android.minifyDebug;
            PlayerSettings.Android.minifyDebug = _MinifyDebug;

            data._PrevMinifyRelease = PlayerSettings.Android.minifyRelease;
            PlayerSettings.Android.minifyRelease = _MinifyRelease;
            
            data._PrevSplitAppBinary = PlayerSettings.Android.splitApplicationBinary;
            PlayerSettings.Android.splitApplicationBinary = _SplitApplicationBinary;

            data._PrevSymbolLevel = UserBuildSettings.DebugSymbols.level;
            UserBuildSettings.DebugSymbols.level = _SymbolLevel;

            data._PrevSymbolFormat = UserBuildSettings.DebugSymbols.format;
            UserBuildSettings.DebugSymbols.format = SymbolFormat;
            
            data._PrevEnableCloudDiagnostics = CrashReportingSettings.enabled;
            if (_ForceDisableCloudDiagnostics) CrashReportingSettings.enabled = false;
        }

        public override IEnumerator OnStateRestore(BuildContext context)
        {
            yield return base.OnStateRestore(context);
            if (!context.TryGetData<Data>(out var data))
                yield break;
            
            PlayerSettings.SetScriptingBackend(NamedBuildTarget, data._PrevBackend);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget, data._PrevIl2CppConfig);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget, data._PrevIl2CppCodeGeneration);

            EditorUserBuildSettings.androidBuildSystem = data._PrevSystem;
            PlayerSettings.Android.targetArchitectures = data._PrevArchitectures;

            EditorUserBuildSettings.buildAppBundle = data._PrevBuildAppBundle;
            PlayerSettings.Android.minifyDebug = data._PrevMinifyDebug;
            PlayerSettings.Android.minifyRelease = data._PrevMinifyRelease;
            PlayerSettings.Android.splitApplicationBinary = data._PrevSplitAppBinary;
            UserBuildSettings.DebugSymbols.level = data._PrevSymbolLevel;
            UserBuildSettings.DebugSymbols.format = data._PrevSymbolFormat;
            
            CrashReportingSettings.enabled = data._PrevEnableCloudDiagnostics;
        }
        
        [Serializable]
        private class Data : ABuildContextData
        {
            [SerializeField]
            public AndroidBuildSystem _PrevSystem;

            [SerializeField]
            public ScriptingImplementation _PrevBackend;

            [SerializeField]
            public Il2CppCompilerConfiguration _PrevIl2CppConfig;

            [SerializeField]
            public Il2CppCodeGeneration _PrevIl2CppCodeGeneration;

            [SerializeField]
            public AndroidArchitecture _PrevArchitectures;

            [SerializeField]
            public bool _PrevBuildAppBundle;

            [SerializeField]
            public bool _PrevSplitAppBinary;

            [SerializeField]
            public bool _PrevMinifyDebug;

            [SerializeField]
            public bool _PrevMinifyRelease;
            
            [SerializeField]
            public DebugSymbolFormat _PrevSymbolFormat;

            [SerializeField]
            public DebugSymbolLevel _PrevSymbolLevel;
            
            [SerializeField]
            public bool _PrevEnableCloudDiagnostics;
        }
    }
    
    public enum DebugSymbolsOutputFormat
    {
        [LabelText(".zip")] Zip = 1,
        IncludeInBundle = 2,
        [LabelText(".zip & Include In Bundle")] ZipAndIncludeInBundle = 3,
    }
    
    public enum DebugSymbolFileExtension
    {
        [LabelText(".so")] Standard = 0,
        [LabelText(".so.sym")] Legacy = 4,
    }
}
#endif