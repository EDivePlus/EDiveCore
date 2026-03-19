using System.Collections.Generic;
using EDIVE.EditorUtils;
using Sirenix.OdinInspector;
using Unity.Android.Types;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using AndroidArchitecture = UnityEditor.AndroidArchitecture;
using AndroidBuildSystem = UnityEditor.AndroidBuildSystem;

namespace EDIVE.BuildTool.PlatformConfigs.Obsolete
{
    public class AndroidBuildPlatformConfig : BuildPlatformConfig, ISelfValidator
    {
    #region Migration
        [SerializeField, HideInInspector] private AndroidArchitecture _TargetArchitectures;
        [SerializeField, HideInInspector] private AndroidBuildSystem _BuildSystem;
        [SerializeField, HideInInspector] private ScriptingImplementation _ScriptingImplementation;
        [SerializeField, HideInInspector] private Il2CppCompilerConfiguration _Il2CppConfig;
        [SerializeField, HideInInspector] private Il2CppCodeGeneration _Il2CppCodeGeneration;
        [SerializeField, HideInInspector] private bool _BuildAndroidAppBundle;
        [SerializeField, HideInInspector] private bool _ExtractAppBundleApk = true;
        [SerializeField, HideInInspector]  private bool _MinifyDebug;
        [SerializeField, HideInInspector] private bool _MinifyRelease;
        [SerializeField, HideInInspector] private bool _SplitApplicationBinary;
        [SerializeField, HideInInspector] private DebugSymbolLevel _SymbolLevel;
        [SerializeField, HideInInspector] private DebugSymbolFormatFlags _SymbolFormat;
        [SerializeField, HideInInspector] private bool _ForceDisableCloudDiagnostics;
        
        public void Validate(SelfValidationResult result)
        {
            result.AddError("AndroidBuildPlatformConfig is obsolete, migrate to base class")
                .WithFix(() => 
                { 
                    this.ChangeScriptType<BuildPlatformConfig>(config =>
                    {
                        var json = JsonUtility.ToJson(this);
                        config._BuildTargetModule = JsonUtility.FromJson<AndroidBuildPlatformModule>(json);
                        config._AdditionalModules = new List<APlatformModule> {JsonUtility.FromJson<PathPlatformModule>(json)};
                    });
                });
        }
    #endregion
    }
}
