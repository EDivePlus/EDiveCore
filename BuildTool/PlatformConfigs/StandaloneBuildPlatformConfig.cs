using System.Collections.Generic;
using EDIVE.EditorUtils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.PlatformConfigs
{
    public class StandaloneBuildPlatformConfig : BuildPlatformConfig, ISelfValidator
    {
    #region Migration
        [SerializeField, HideInInspector] private StandaloneBuildPlatformModule.StandalonePlatform _Platform;
        [SerializeField, HideInInspector] private StandaloneBuildPlatformModule.StandaloneArchitecture _Architecture;
        [SerializeField, HideInInspector] private StandaloneBuildPlatformModule.StandaloneTargetMode _TargetMode;
        [SerializeField, HideInInspector] private ScriptingImplementation _ScriptingImplementation;
        [SerializeField, HideInInspector] private Il2CppCompilerConfiguration _Il2CppConfig;
        [SerializeField, HideInInspector] private Il2CppCodeGeneration _Il2CppCodeGeneration; 
        
        public void Validate(SelfValidationResult result)
        {
            result.AddError("AndroidBuildPlatformConfig is obsolete, migrate to base class")
                .WithFix(() => 
                { 
                    this.ChangeScriptType<BuildPlatformConfig>(config =>
                    {
                        var json = JsonUtility.ToJson(this);
                        config._BaseModule = JsonUtility.FromJson<StandaloneBuildPlatformModule>(json);
                        config._AdditionalModules = new List<APlatformModule> {JsonUtility.FromJson<PathPlatformModule>(json)};
                    });
                });
        }
    #endregion
    }
}
