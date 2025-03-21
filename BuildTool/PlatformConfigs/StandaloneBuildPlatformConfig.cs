using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.PlatformConfigs
{
    public class StandaloneBuildPlatformConfig : ABuildPlatformConfig
    {
        [EnhancedBoxGroup("Backend", "@ColorTools.Purple", Order = -1)]
        [SerializeField]
        private StandalonePlatform _Platform;

        [EnhancedBoxGroup("Backend")]
        [ShowIf(nameof(_Platform), StandalonePlatform.Windows)]
        [SerializeField]
        private StandaloneArchitecture _Architecture;

        [EnhancedBoxGroup("Backend")]
        [SerializeField]
        private StandaloneTargetMode _TargetMode;

        [PropertySpace(5)]
        [EnhancedBoxGroup("Backend")]
        [SerializeField]
        private ScriptingImplementation _ScriptingImplementation = ScriptingImplementation.IL2CPP;

        [EnhancedBoxGroup("Backend")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [SerializeField]
        private Il2CppCompilerConfiguration _Il2CppConfig = Il2CppCompilerConfiguration.Release;

        [EnhancedBoxGroup("Backend")]
        [ShowIf("ScriptingImplementation", ScriptingImplementation.IL2CPP)]
        [SerializeField]
        [PropertyTooltip("IL2CPP compiler will generate code optimized for:\nOptimizeSpeed - runtime performance.\nOptimizeSize - size and build time")]
        private Il2CppCodeGeneration _Il2CppCodeGeneration = Il2CppCodeGeneration.OptimizeSpeed;

        public ScriptingImplementation ScriptingImplementation => _ScriptingImplementation;
        public Il2CppCompilerConfiguration Il2CppConfig => _Il2CppConfig;
        public Il2CppCodeGeneration Il2CppCodeGeneration => _Il2CppCodeGeneration;

        public enum StandalonePlatform
        {
            Windows,
            Linux,
            MacOS
        }

        public enum StandaloneArchitecture
        {
            X64,
            X86
        }

        public enum StandaloneTargetMode
        {
            Player,
            Server
        }

        public override NamedBuildTarget NamedBuildTarget => _TargetMode == StandaloneTargetMode.Server ? NamedBuildTarget.Server : NamedBuildTarget.Standalone;
        public override BuildTarget BuildTarget => _Platform switch
        {
            StandalonePlatform.Windows => _Architecture == StandaloneArchitecture.X64 ? BuildTarget.StandaloneWindows64 : BuildTarget.StandaloneWindows,
            StandalonePlatform.Linux => BuildTarget.StandaloneLinux64,
            StandalonePlatform.MacOS => BuildTarget.StandaloneOSX,
            _ => throw new ArgumentOutOfRangeException()
        };

        public override string BuildExtension => _Platform switch
        {
            StandalonePlatform.Windows => ".exe",
            StandalonePlatform.Linux => ".app",
            StandalonePlatform.MacOS => ".x86_64",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
