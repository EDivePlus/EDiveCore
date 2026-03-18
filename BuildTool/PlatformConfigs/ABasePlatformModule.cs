// Author: František Holubec
// Created: 18.03.2026

using System;
using EDIVE.BuildTool.Utils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public abstract class ABasePlatformModule : APlatformModule
    {
        public override int ExecutionOrder => -10000;
        
        [EnhancedBoxGroup("Backend")]
        [SerializeField]
        protected ScriptingImplementation _ScriptingImplementation = ScriptingImplementation.IL2CPP;
        public ScriptingImplementation ScriptingImplementation => _ScriptingImplementation;

        [EnhancedBoxGroup("Backend")]
        [ShowIf(nameof(_ScriptingImplementation), ScriptingImplementation.IL2CPP)]
        [LabelText("IL2CPP Config")]
        [SerializeField]
        protected Il2CppCompilerConfiguration _Il2CppConfig = Il2CppCompilerConfiguration.Release;
        public Il2CppCompilerConfiguration Il2CppConfig => _Il2CppConfig;

        [EnhancedBoxGroup("Backend")]
        [ShowIf(nameof(_ScriptingImplementation), ScriptingImplementation.IL2CPP)]
        [PropertyTooltip("IL2CPP compiler will generate code optimized for:\nOptimizeSpeed - runtime performance.\nOptimizeSize - size and build time")]
        [LabelText("IL2CPP Code Generation")]
        [SerializeField]
        protected Il2CppCodeGeneration _Il2CppCodeGeneration = Il2CppCodeGeneration.OptimizeSpeed;
        public Il2CppCodeGeneration Il2CppCodeGeneration => _Il2CppCodeGeneration;
        
        [EnhancedBoxGroup("Build", "@ColorTools.Cyan")]
        [SerializeField]
        protected bool _DevelopmentBuild;
        public bool DevelopmentBuild => _DevelopmentBuild;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _AllowDebugging;
        public bool AllowDebugging => _AllowDebugging;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _WaitForManagedDebugger;
        public bool WaitForManagedDebugger => _WaitForManagedDebugger;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _AutoConnectProfiler;
        public bool AutoConnectProfiler => _AutoConnectProfiler;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _EnableDeepProfile;
        public bool EnableDeepProfile => _EnableDeepProfile;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _CleanBuildCache;
        public bool CleanBuildCache => _CleanBuildCache;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _DetailedBuildReport;
        public bool DetailedBuildReport => _DetailedBuildReport;
        
        [EnhancedBoxGroup("Stripping", "@ColorTools.Red", SpaceBefore = 4)]
        [SerializeField]
        private bool _StripEngineCode = true;
        public bool StripEngineCode => _StripEngineCode;

        [EnhancedBoxGroup("Stripping")]
        [SerializeField]
        private ManagedStrippingLevel _ManagedStrippingLevel = ManagedStrippingLevel.Low;
        public ManagedStrippingLevel ManagedStrippingLevel => _ManagedStrippingLevel;

        [EnhancedBoxGroup("Stripping")]
        [SerializeField]
        private PlayerCompressionType _PlayerCompression = PlayerCompressionType.Default;
        public PlayerCompressionType PlayerCompression => _PlayerCompression;

        [EnhancedBoxGroup("Stripping")]
        [SerializeField]
        private bool _UseIncrementalGC = true;
        public bool UseIncrementalGC => _UseIncrementalGC;
        
        [EnhancedBoxGroup("Logging","@ColorTools.Yellow", SpaceBefore = 4)]
        [SerializeField]
        [HideLabel]
        [InlineProperty]
        private LoggingSetup _LoggingSetup;
        public LoggingSetup LoggingSetup => _LoggingSetup;
        
        public abstract NamedBuildTarget NamedBuildTarget { get; }
        public abstract BuildTarget BuildTarget { get; }
        public abstract string BuildExtension { get; }

        public override void SetupBeforeBuild(BuildContext context)
        {
            var data = context.GetOrCreateData<Data>();

            data._PrevBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget, _ScriptingImplementation);

            data._PrevIl2CppConfig = PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget, _Il2CppConfig);

            data._PrevIl2CppCodeGeneration = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget, _Il2CppCodeGeneration);
            
            data._PrevStripEngineCode = PlayerSettings.stripEngineCode;
            PlayerSettings.stripEngineCode = _StripEngineCode;

            data._PrevManagedStrippingLevel = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget, _ManagedStrippingLevel);

            data._PrevLogs = LoggingSetup.GetCurrent();
            _LoggingSetup.Apply();

            data._PrevWaitForManagedDebugger = EditorUserBuildSettings.waitForManagedDebugger;
            EditorUserBuildSettings.waitForManagedDebugger = _WaitForManagedDebugger;

            data._PrevEnableDeepProfile = EditorUserBuildSettings.buildWithDeepProfilingSupport;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = _EnableDeepProfile;

            data._PrevAutoConnectProfiler = EditorUserBuildSettings.connectProfiler;
            EditorUserBuildSettings.connectProfiler = _AutoConnectProfiler;

            data._PrevGCIncremental = PlayerSettings.gcIncremental;
            PlayerSettings.gcIncremental = _UseIncrementalGC;
            
            if (_DevelopmentBuild) context.Options |= BuildOptions.Development;
            if (_AllowDebugging) context.Options |= BuildOptions.AllowDebugging;
            if (_CleanBuildCache) context.Options |= BuildOptions.CleanBuildCache;
            if (_DetailedBuildReport) context.Options |= BuildOptions.DetailedBuildReport;
            context.Options |= _PlayerCompression.ToBuildOptions();
        }

        public override void RestoreAfterBuild(BuildContext context)
        {
            if (!context.TryGetData<Data>(out var data))
                return;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget, data._PrevBackend);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget, data._PrevIl2CppConfig);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget, data._PrevIl2CppCodeGeneration);
            
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget, data._PrevManagedStrippingLevel);
            EditorUserBuildSettings.waitForManagedDebugger = data._PrevWaitForManagedDebugger;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = data._PrevEnableDeepProfile;
            EditorUserBuildSettings.connectProfiler = data._PrevAutoConnectProfiler;
            PlayerSettings.gcIncremental = data._PrevGCIncremental;
            PlayerSettings.stripEngineCode = data._PrevStripEngineCode;
            data._PrevLogs.Apply();
        }
        
        [Serializable]
        private class Data : ABuildContextData
        {
            [SerializeField]
            public ScriptingImplementation _PrevBackend;

            [SerializeField]
            public Il2CppCompilerConfiguration _PrevIl2CppConfig;

            [SerializeField]
            public Il2CppCodeGeneration _PrevIl2CppCodeGeneration;
            
            [SerializeField]
            public LoggingSetup _PrevLogs;

            [SerializeField]
            public bool _PrevStripEngineCode;

            [SerializeField]
            public ManagedStrippingLevel _PrevManagedStrippingLevel;

            [SerializeField]
            public bool _PrevWaitForManagedDebugger;

            [SerializeField]
            public bool _PrevGCIncremental;

            [SerializeField]
            public bool _PrevEnableDeepProfile;

            [SerializeField]
            public bool _PrevAutoConnectProfiler;
        }
    }
}
