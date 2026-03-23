using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Utils;
using EDIVE.EditorUtils.DomainReload;
using EDIVE.NativeUtils;
using EDIVE.NativeUtils.TeamCity;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.BuildTool
{
    [Serializable]
    public class BuildRunner
    {
        private const string DOMAIN_RELOAD_SURVIVOR_ID = "BuildRunner";
        
        [SerializeField]
        protected BuildPreset _Preset;
        
        [SerializeField]
        private BuildContext _Context;

        [SerializeField]
        private string[] _PrevDefines;

        [SerializeField]
        private BuildTarget _PrevBuildTarget;

        [SerializeField]
        private string _PrevNamedBuildTargetName;

        public BuildPreset Preset => _Preset;
        public BuildContext Context => _Context;
        
        public BuildUserConfig UserConfig => Preset.UserConfig;
        public BuildPlatformConfig PlatformConfig => Preset.PlatformConfig;
        
        private EditorCoroutine _buildCoroutine;

        public BuildRunner() { }
        public BuildRunner(BuildPreset preset, BuildOptions options = BuildOptions.None)
        {
            _Preset = preset;
            _Context = new BuildContext(options)
            {
                PlatformConfig = preset.PlatformConfig,
                UserConfig = preset.UserConfig
            };
        }
        
        public void StartBuild()
        {
            DomainReloadUtility.RegisterSurvivor(DOMAIN_RELOAD_SURVIVOR_ID, new BuildRunnerDomainReloadSurvivor(this));
            TeamCityServiceMessages.MessageLog("[BuildRunner] Starting...");
            _buildCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(StartBuildRoutine());
        }

        public void KillBuild()
        {
            EditorCoroutineUtility.StopCoroutine(_buildCoroutine);
        }
 
        private IEnumerator StartBuildRoutine()
        {
            yield return DomainReloadUtility.WaitWhileCompiling();
            DomainReloadUtility.ClearSurvivor(DOMAIN_RELOAD_SURVIVOR_ID);
            
            var buildTarget = PlatformConfig.BuildTarget;
            if (Context.State == BuildStateType.NotStarted)
            {
                TeamCityServiceMessages.SetParameter("UnityBuild.ProductName", PlayerSettings.productName);
                TeamCityServiceMessages.SetParameter("UnityBuild.UnityEditorVersion", Application.unityVersion);
                
                if (!BuildUtils.IsBuildTargetSupported(buildTarget))
                {
                    Debug.LogError($"[BuildRunner] Build target {buildTarget} is not supported");
                    TeamCityServiceMessages.MessageBuildProblem($"[BuildRunner] Build target {buildTarget} is not supported! Check if it is installed for current version of Unity on building machine!");
                    yield break;
                }
                
                DomainReloadUtility.RegisterSurvivor(DOMAIN_RELOAD_SURVIVOR_ID, new BuildRunnerDomainReloadSurvivor(this));
                yield return DomainReloadUtility.WaitWhileCompiling();
                DomainReloadUtility.ClearSurvivor(DOMAIN_RELOAD_SURVIVOR_ID);
                
                DebugLite.Log("[BuildRunner] Initial build preparation...");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    yield break;
                
                var activeScene = SceneManager.GetActiveScene();
                if (!string.IsNullOrEmpty(activeScene.path))
                {
                    EditorSceneManager.OpenScene(activeScene.path);
                }
                EditorUtility.DisplayProgressBar("Build", "Build initializing", 0f);
            }
            
            DebugLite.Log("[BuildRunner] Build prepared for start or resume...");

            if (Context.State != BuildStateType.NotStarted)
            {
                DebugLite.Log($"[BuildRunner] Resuming build from state {Context.State}");
            }
        
            if (Context.State < BuildStateType.StateCapture)
            {
                EditorUtility.DisplayProgressBar("Build", "Editor state capture", 0.1f);
                yield return ExecuteBuildSegment(CaptureAndChangeEditorState);
            }
                
            if (Context.State <  BuildStateType.Preprocess)
            {
                EditorUtility.DisplayProgressBar("Build", "Pre processing build", 0.1f);
                yield return ExecuteBuildSegment(PreprocessBuild);
            }

            if (Context.State < BuildStateType.PipelinePreparation)
            {
                EditorUtility.DisplayProgressBar("Build", "Build binary", 0.2f);
                yield return ExecuteBuildSegment(BuildBinary);
            }

            if (Context.State < BuildStateType.Postprocess)
            {
                EditorUtility.DisplayProgressBar("Build", "Post processing build", 0.9f);
                yield return ExecuteBuildSegment(PostProcessAndChangeEditor);
            }
        
            if (Context.State < BuildStateType.StateRestore)
            {
                EditorUtility.DisplayProgressBar("Build", "Editor state restoring", 0.9f);
                yield return ExecuteBuildSegment(RestoreEditorState);
            }

            EditorUtility.ClearProgressBar();
            
            if (Application.isBatchMode)
                EditorApplication.Exit(_Context.Result == BuildResult.Succeeded ? 0 : 1);
        }

        private IEnumerator ExecuteBuildSegment(Func<IEnumerator> segmentFunction)
        {
            EditorApplication.LockReloadAssemblies();
            yield return segmentFunction();
            DomainReloadUtility.RegisterSurvivor(DOMAIN_RELOAD_SURVIVOR_ID, new BuildRunnerDomainReloadSurvivor(this));
            EditorApplication.UnlockReloadAssemblies();
            yield return DomainReloadUtility.WaitWhileCompiling();
            DomainReloadUtility.ClearSurvivor(DOMAIN_RELOAD_SURVIVOR_ID);
        }
        
        private IEnumerator CaptureAndChangeEditorState()
        {
            SetContextState(BuildStateType.StateCapture);

            Preset.Validate();
            yield return null;

            Context.VersionDefinition = BuildGlobalSettings.Instance.VersionDefinition;
            if (!Application.isBatchMode)
                Context.VersionDefinition.IncrementCurrentVersion();
            Context.VersionDefinition.ApplyCurrentVersion();

            Context.ResultPath = UserConfig.PathResolver.ResolvePath(Preset);
            Context.Defines = Preset.GetBuildDefines(Context).ToList();
            PlayerSettings.GetScriptingDefineSymbols(PlatformConfig.NamedBuildTarget, out _PrevDefines);
            
            TeamCityServiceMessages.SetParameter("UnityBuild.ResultFolderPath", Context.ResultPath.FolderPath);
            TeamCityServiceMessages.SetParameter("UnityBuild.ResultFileName", Context.ResultPath.FileName);
            TeamCityServiceMessages.SetParameter("UnityBuild.ResultFullPath", Context.ResultPath.FullPath);
            TeamCityServiceMessages.SetParameter("UnityBuild.ResultVersion", Context.VersionDefinition.VersionString);
            
            TeamCityServiceMessages.SetBuildNumber(Context.VersionDefinition.VersionString);
            
            Context.Scenes = PlatformConfig.OverrideSceneList != null && PlatformConfig.OverrideSceneList.Scenes.Count > 0 
                ? PlatformConfig.OverrideSceneList.Scenes 
                : EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            
            DebugLite.Log("[BuildRunner] StateCapture Actions executing");
            yield return ExecuteBuildCallback<IStateCaptureBuildCallback>(Preset.GetBuildCallbacks(Context), c => c.OnStateCapture(_Context));
            DebugLite.Log("[BuildRunner] StateCapture Actions completed");

            DebugLite.Log("[BuildRunner] Applying settings");
            SetDefineSymbols(PlatformConfig.NamedBuildTarget, _Context.Defines);

            SetContextState(BuildStateType.BuildTargetSwitch);
            _PrevBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            _PrevNamedBuildTargetName = BuildUtils.CurrentNamedBuildTarget.TargetName;
            EditorUserBuildSettings.SwitchActiveBuildTarget(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget);

            CompilationPipeline.RequestScriptCompilation();
        }

        private IEnumerator PreprocessBuild()
        {
            SetContextState(BuildStateType.Preprocess);
            
            DebugLite.Log("[BuildRunner] Preprocess Actions executing");
            yield return ExecuteBuildCallback<IPreprocessBuildCallback>(Preset.GetBuildCallbacks(Context), buildAction => buildAction.OnPreprocess(_Context));
            DebugLite.Log("[BuildRunner] Preprocess Actions completed");
            
            PathUtility.EnsurePathExists(Context.ResultPath.FullPath);
        }

        private IEnumerator BuildBinary()
        {
            yield return null;
            
            SetContextState(BuildStateType.PipelinePreparation);
            DebugLite.Log($"[BuildRunner] Starting build (Path: {Context.ResultPath.FullPath})");

            try
            {
                SetContextState(BuildStateType.PipelineInProgress);
                _Context.Report = BuildPipeline.BuildPlayer(Context.Scenes.ToArray(), Context.ResultPath.FullPath, PlatformConfig.BuildTarget, Context.Options);
                _Context.Result = _Context.Report.summary.result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _Context.Result = BuildResult.Failed;
            }
            finally
            {
                SetContextState(BuildStateType.PipelineFinalization);
            }

            DebugLite.Log($"[BuildRunner] Build Completed with result: {_Context.Result}");
        }

        private IEnumerator PostProcessAndChangeEditor()
        {
            SetContextState(BuildStateType.Postprocess);

            DebugLite.Log("[BuildRunner] Postprocess Actions executing");
            yield return ExecuteBuildCallback<IPostprocessBuildCallback>(Preset.GetBuildCallbacks(Context), buildAction => buildAction.OnPostprocess(_Context));
            DebugLite.Log("[BuildRunner] Postprocess Actions completed");

            if (Application.isBatchMode)
            {
                DebugLite.Log("[BuildRunner] Target switch is ignored in batch mode");
                yield break;
            }
            
            SetContextState(BuildStateType.BuildTargetRevert);
            
            DebugLite.Log("[BuildRunner] Restoring settings");
            SetDefineSymbols(PlatformConfig.NamedBuildTarget, _PrevDefines);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildUtils.GetNamedBuildTarget(_PrevNamedBuildTargetName), _PrevBuildTarget);
            CompilationPipeline.RequestScriptCompilation();
        }

        private IEnumerator RestoreEditorState()
        {
            if (Application.isBatchMode)
            {
                DebugLite.Log("[BuildRunner] Restore is ignored in batch mode");
                yield break;
            }

            SetContextState(BuildStateType.StateRestore);
            
            DebugLite.Log("[BuildRunner] Actions pre defines executing");
            yield return ExecuteBuildCallback<IStateRestoreBuildCallback>(Preset.GetBuildCallbacks(Context), buildAction => buildAction.OnStateRestore(_Context));
            DebugLite.Log("[BuildRunner] Actions pre defines completed");

            DebugLite.Log("[BuildRunner] Build Postprocess Completed");
            SetContextState(BuildStateType.Completed);

            if (_Context.Result != BuildResult.Unknown)
            {
                if (_Context.Result == BuildResult.Succeeded)
                    DebugLite.Log($"[BuildRunner] {PlatformConfig.NamedBuildTarget} Build completed");
                else
                    Debug.LogError($"[BuildRunner] {PlatformConfig.NamedBuildTarget} Build result: '{_Context.Report.summary.result}'");
            }
        }

        private void SetDefineSymbols(NamedBuildTarget namedTarget, IEnumerable<string> defines)
        {
            DebugLite.Log($"[BuildRunner] Previous defines: {PlayerSettings.GetScriptingDefineSymbols(namedTarget)}");
            var definesArray = defines.ToArray();
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, definesArray);
            AssetDatabase.SaveAssets();
            DebugLite.Log($"[BuildRunner] New defines: {string.Join(",", definesArray)}");
        }

        private static IEnumerator ExecuteBuildCallback<TBuildCallback>(IEnumerable<IBuildCallback> callbacks, Func<TBuildCallback, IEnumerator> function) 
            where TBuildCallback : IBuildCallback
        {
            foreach (var callback in callbacks.Where(c => c != null).OrderBy(c => c, IBuildCallback.PriorityComparer))
            {
                if (callback is not TBuildCallback tCallback)
                    continue;

                DebugLite.Log($"[BuildRunner] Executing build action {callback.CallbackName}");
                yield return function(tCallback);
            }
        }
        
        private void SetContextState(BuildStateType state)
        {
            if (Context.State == state)
                return;
            
            if (Context.State != BuildStateType.NotStarted)
            {
                DebugLite.Log($"[BuildRunner] State completed {Context.State}");
                TeamCityServiceMessages.EndMessageBlock($"[BuildRunner] State: {Context.State}");
            }
            
            Context.State = state;
            if (Context.State != BuildStateType.Completed)
            {
                TeamCityServiceMessages.BeginMessageBlock($"[BuildRunner] State: {Context.State}");
                DebugLite.Log($"[BuildRunner] State started {Context.State}");
            }
        }
    }
}
