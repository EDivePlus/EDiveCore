// Author: František Holubec
// Created: 18.03.2026

using System;
using System.Collections;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.CrashReporting;
using UnityEngine;
using AndroidArchitecture = UnityEditor.AndroidArchitecture;
using AndroidBuildSystem = UnityEditor.AndroidBuildSystem;

#if UNITY_ANDROID
using UnityEditor.Android;
using Unity.Android.Types;
#endif

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public class AndroidBuildPlatformModule : ABuildTargetPlatformModule
    {
        public override string PlatformName => "Android";
        
#pragma warning disable CS0414
        [EnhancedBoxGroup("Backend")]
        [SerializeField]
        private AndroidArchitecture _TargetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        [EnhancedBoxGroup("Backend")]
        [SerializeField]
        private AndroidBuildSystem _BuildSystem = AndroidBuildSystem.Gradle;
        
        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _BuildAndroidAppBundle;

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

        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [InfoBox("Unity forces Full symbols when CloudDiagnostics is enabled", InfoMessageType.Warning, nameof(ShowForcedSymbolsMessage))]
        [SerializeField]
        private DebugSymbolLevelCustom _SymbolLevel = DebugSymbolLevelCustom.None;

        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [SerializeField]
        private DebugSymbolsOutputFormat _SymbolOutputFormat = DebugSymbolsOutputFormat.ZipAndIncludeInBundle;
        
        [EnhancedBoxGroup("Build")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [SerializeField]
        private DebugSymbolFileExtension _SymbolFileExtension = DebugSymbolFileExtension.Standard;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _ForceDisableCloudDiagnostics;
#pragma warning restore CS0414

        private bool ShowForcedSymbolsMessage => CrashReportingSettings.enabled && !_ForceDisableCloudDiagnostics && _SymbolLevel != DebugSymbolLevelCustom.Full;
        
#if UNITY_ANDROID
        public AndroidArchitecture TargetArchitectures => _TargetArchitectures;
        public AndroidBuildSystem BuildSystem => _BuildSystem;
        public bool BuildAndroidAppBundle => _BuildAndroidAppBundle;
        public bool MinifyDebug => _MinifyDebug;
        public bool MinifyRelease => _MinifyRelease;
        public bool SplitApplicationBinary => _SplitApplicationBinary;
        public DebugSymbolLevel SymbolLevel => (DebugSymbolLevel) _SymbolLevel;
        public bool ForceDisableCloudDiagnostics => _ForceDisableCloudDiagnostics;
        public DebugSymbolFormat SymbolFormat => (DebugSymbolFormat)((int) _SymbolOutputFormat | (int) _SymbolFileExtension);

#endif

        public override NamedBuildTarget NamedBuildTarget => NamedBuildTarget.Android;
        public override BuildTarget BuildTarget => BuildTarget.Android;
        public override string BuildExtension => _BuildAndroidAppBundle ? ".aab" : ".apk";

        public override IEnumerator OnStateCapture(BuildContext context)
        {
#if UNITY_ANDROID
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
            UserBuildSettings.DebugSymbols.level = SymbolLevel;

            data._PrevSymbolFormat = UserBuildSettings.DebugSymbols.format;
            UserBuildSettings.DebugSymbols.format = SymbolFormat;
            
            data._PrevEnableCloudDiagnostics = CrashReportingSettings.enabled;
            if (_ForceDisableCloudDiagnostics) CrashReportingSettings.enabled = false;
#endif
            yield break;
        }

        public override IEnumerator OnStateRestore(BuildContext context)
        {
#if UNITY_ANDROID
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
#endif
            yield break;
        }
        
#if UNITY_ANDROID
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
#endif
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
    
    public enum DebugSymbolLevelCustom
    {
        None = 1,
        SymbolTable = 2,
        Full = 4,
    }
}