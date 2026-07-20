// Author: František Holubec
// Created: 18.03.2026

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.Utils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public abstract class ABuildTargetPlatformModule : IPlatformModule, IStateCaptureBuildCallback, IStateRestoreBuildCallback
    {
        public abstract string PlatformName { get; }
        public virtual int Priority => -100;
        public string CallbackName => $"Build Target {PlatformName}";

        [EnhancedBoxGroup("Backend", "@ColorTools.Purple", Order = -1, SpaceAfter = 4)]
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

        [EnhancedBoxGroup("Backend")]
        [PropertySpace(4)]
        [LabelText("Auto Graphics API")]
        [SerializeField]
        protected bool _AutoGraphicsAPI = true;
        public bool AutoGraphicsAPI => _AutoGraphicsAPI;

        [EnhancedBoxGroup("Backend")]
        [HideIf(nameof(_AutoGraphicsAPI))]
        [LabelText("Graphics APIs")]
        [ShowAsString]
        [ValueDropdown(nameof(GetSupportedGraphicsAPIsDropdown), IsUniqueList = true, DrawDropdownForListElements = false)]
        [EnhancedValidate(nameof(ValidateGraphicsAPIs))]
        [SerializeField]
        protected List<GraphicsDeviceType> _GraphicsAPIs = new();
        public IReadOnlyList<GraphicsDeviceType> GraphicsAPIs => _GraphicsAPIs;

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
        public BuildTargetGroup BuildTargetGroup => BuildPipeline.GetBuildTargetGroup(BuildTarget);
        public abstract string BuildExtension { get; }
        
        public virtual IEnumerator OnStateCapture(BuildContext context)
        {
             var data = context.GetOrCreateData<Data>();

            data._PrevBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget, _ScriptingImplementation);

            data._PrevIl2CppConfig = PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget, _Il2CppConfig);

            data._PrevIl2CppCodeGeneration = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget, _Il2CppCodeGeneration);

            data._PrevAutoGraphicsAPI = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget);
            data._PrevGraphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget);
            ApplyGraphicsAPIs(_AutoGraphicsAPI, _GraphicsAPIs);

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
            yield break;
        }
        
        public virtual IEnumerator OnStateRestore(BuildContext context)
        {
            if (!context.TryGetData<Data>(out var data))
                yield break;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget, data._PrevBackend);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget, data._PrevIl2CppConfig);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget, data._PrevIl2CppCodeGeneration);

            RestoreGraphicsAPIs(data._PrevAutoGraphicsAPI, data._PrevGraphicsAPIs);

            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget, data._PrevManagedStrippingLevel);
            EditorUserBuildSettings.waitForManagedDebugger = data._PrevWaitForManagedDebugger;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = data._PrevEnableDeepProfile;
            EditorUserBuildSettings.connectProfiler = data._PrevAutoConnectProfiler;
            PlayerSettings.gcIncremental = data._PrevGCIncremental;
            PlayerSettings.stripEngineCode = data._PrevStripEngineCode;
            data._PrevLogs.Apply();
        }

        private void ApplyGraphicsAPIs(bool useDefault, IReadOnlyList<GraphicsDeviceType> apis)
        {
            if (useDefault)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget, true);
                return;
            }

            if (apis == null || apis.Count == 0)
            {
                Debug.LogWarning($"[{PlatformName}] Auto Graphics API is disabled but the custom Graphics APIs list is empty. Falling back to Unity defaults.");
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget, true);
                return;
            }

            var newApis = apis.ToArray();
            var currentApis = PlayerSettings.GetGraphicsAPIs(BuildTarget);
            if (currentApis == null || !currentApis.SequenceEqual(newApis))
                PlayerSettings.SetGraphicsAPIs(BuildTarget, newApis);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget, false);
        }

        private void RestoreGraphicsAPIs(bool prevUseDefault, GraphicsDeviceType[] prevApis)
        {
            if (prevApis != null && prevApis.Length > 0)
            {
                var currentApis = PlayerSettings.GetGraphicsAPIs(BuildTarget);
                if (currentApis == null || !currentApis.SequenceEqual(prevApis))
                    PlayerSettings.SetGraphicsAPIs(BuildTarget, prevApis);
            }
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget, prevUseDefault);
        }
        
        private IEnumerable<GraphicsDeviceType> GetSupportedGraphicsAPIsDropdown()
        {
            return GraphicsAPIUtils.GetSupportedGraphicsAPIs(BuildTarget);
        }

        private void ValidateGraphicsAPIs(List<GraphicsDeviceType> value, SelfValidationResult result)
        {
            if (_AutoGraphicsAPI || value == null)
                return;

            var supported = GraphicsAPIUtils.GetSupportedGraphicsAPIs(BuildTarget);
            var unsupported = value.Where(a => !supported.Contains(a)).ToList();
            if (unsupported.Count > 0)
            {
                result.AddError($"Graphics APIs not supported by {BuildTarget}: {string.Join(", ", unsupported)}")
                    .WithFix(() => value.RemoveAll(a => !supported.Contains(a)));
            }
            else if (value.Count == 0)
            {
                result.AddWarning("Auto Graphics API is disabled but no APIs are listed; Unity defaults will be used.");
            }
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
            public bool _PrevAutoGraphicsAPI;

            [SerializeField]
            public GraphicsDeviceType[] _PrevGraphicsAPIs;

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
