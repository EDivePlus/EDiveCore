using System;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Presets;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.Runners
{
    [Serializable]
    public class StandaloneBuildRunner : ABuildRunner<StandaloneBuildPreset, StandaloneBuildPlatformConfig>
    {
        [SerializeField]
        private ScriptingImplementation _PrevBackend;

        [SerializeField]
        private Il2CppCompilerConfiguration _PrevIl2CppConfig;

        [SerializeField]
        private Il2CppCodeGeneration _PrevIl2CppCodeGeneration;


        public ScriptingImplementation PrevBackend { get => _PrevBackend; set => _PrevBackend = value; }
        public Il2CppCompilerConfiguration PrevIl2CppConfig { get => _PrevIl2CppConfig; set => _PrevIl2CppConfig = value; }
        public Il2CppCodeGeneration PrevIl2CppCodeGeneration { get => _PrevIl2CppCodeGeneration; set => _PrevIl2CppCodeGeneration = value; }

        public StandaloneBuildRunner() { }
        public StandaloneBuildRunner(StandaloneBuildPreset buildPreset, BuildOptions options = BuildOptions.None) : base(buildPreset, options) { }

        protected override void SetupSettingsBeforeBuild()
        {
            base.SetupSettingsBeforeBuild();

            PrevBackend = PlayerSettings.GetScriptingBackend(PlatformConfig.NamedBuildTarget);
            PlayerSettings.SetScriptingBackend(PlatformConfig.NamedBuildTarget, PlatformConfig.ScriptingImplementation);

            PrevIl2CppConfig = PlayerSettings.GetIl2CppCompilerConfiguration(PlatformConfig.NamedBuildTarget);
            PlayerSettings.SetIl2CppCompilerConfiguration(PlatformConfig.NamedBuildTarget, PlatformConfig.Il2CppConfig);

            PrevIl2CppCodeGeneration = PlayerSettings.GetIl2CppCodeGeneration(PlatformConfig.NamedBuildTarget);
            PlayerSettings.SetIl2CppCodeGeneration(PlatformConfig.NamedBuildTarget, PlatformConfig.Il2CppCodeGeneration);
        }

        protected override void RestoreSettingsAfterBuild()
        {
            base.RestoreSettingsAfterBuild();
            PlayerSettings.SetScriptingBackend(PlatformConfig.NamedBuildTarget, PrevBackend);
            PlayerSettings.SetIl2CppCompilerConfiguration(PlatformConfig.NamedBuildTarget, PrevIl2CppConfig);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Standalone, PrevIl2CppCodeGeneration);
        }
    }
}
