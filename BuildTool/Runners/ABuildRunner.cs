using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.Actions;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Presets;
using EDIVE.BuildTool.Utils;
using EDIVE.EditorUtils.DomainReload;
using EDIVE.NativeUtils;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.BuildTool.Runners
{
    [Serializable]
    public abstract class ABuildRunner
    {
        protected const string DOMAIN_RELOAD_SURVIVOR_ID = "BuildRunner";

        [SerializeField]
        protected BuildContext _Context;

        [SerializeField]
        protected string[] _PrevDefines;

        [SerializeField]
        protected LoggingSetup _PrevLogs;

        [SerializeField]
        protected bool _PrevStripEngineCode;

        [SerializeField]
        protected ManagedStrippingLevel _PrevManagedStrippingLevel;

        [SerializeField]
        protected bool _PrevWaitForManagedDebugger;

        [SerializeField]
        protected bool _PrevGCIncremental;

        [SerializeField]
        protected bool _PrevEnableDeepProfile;

        [SerializeField]
        protected bool _PrevAutoConnectProfiler;

        [SerializeField]
        protected BuildTarget _PrevBuildTarget;

        [SerializeField]
        protected bool _PrevWasServer;

        public BuildContext Context => _Context;

        private EditorCoroutine _buildCoroutine;

        public void StartBuild()
        {
            _buildCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(StartBuildRoutine());
        }

        public void KillBuild()
        {
            EditorCoroutineUtility.StopCoroutine(_buildCoroutine);
        }

        protected abstract IEnumerator StartBuildRoutine();
    }

    [Serializable]
    public abstract class ABuildRunner<TPreset, TPlatformConfig> : ABuildRunner
        where TPreset : ABuildPreset<TPlatformConfig>
        where TPlatformConfig : ABuildPlatformConfig
    {
        [SerializeField]
        protected TPreset _Preset;

        public TPreset Preset => _Preset;
        public BuildUserConfig UserConfig => Preset.UserConfig;
        public TPlatformConfig PlatformConfig => Preset.PlatformConfig;

        protected ABuildRunner() { }
        protected ABuildRunner(TPreset preset, BuildOptions options = BuildOptions.None)
        {
            _Preset = preset;
            _Context = new BuildContext(options);
        }

        protected override IEnumerator StartBuildRoutine()
        {
            if (Context.State != BuildStateType.NotStarted)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    yield break;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.OpenScene(activeScene.path);
            }
            
            if (!BuildUtils.IsBuildTargetSupported(PlatformConfig.BuildTarget))
            {
                Debug.LogError($"[BuildRunner] Build target {PlatformConfig.BuildTarget} is not supported");
                yield break;
            }
                
            EditorUtility.DisplayProgressBar("Build", "Build initializing", 0f);
            DebugLite.Log("[BuildRunner] Build prepared for start or resume...");

            yield return null;
            while (EditorApplication.isCompiling)
                yield return null;
            
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
        }

        private IEnumerator ExecuteBuildSegment(Func<IEnumerator> segmentFunction)
        {
            DebugLite.Log($"[BuildRunner] State started {Context.State}");
            EditorApplication.LockReloadAssemblies();
            yield return segmentFunction();
            DomainReloadUtility.RegisterSurvivor(DOMAIN_RELOAD_SURVIVOR_ID, new BuildRunnerDomainReloadSurvivor(this));
            EditorApplication.UnlockReloadAssemblies();
            yield return null;
            while (EditorApplication.isCompiling)
                yield return null;
            DebugLite.Log($"[BuildRunner] State completed {Context.State}");
            DomainReloadUtility.ClearSurvivor(DOMAIN_RELOAD_SURVIVOR_ID);
        }
        
        private IEnumerator CaptureAndChangeEditorState()
        {
            Context.State = BuildStateType.StateCapture;

            Preset.Validate();
            yield return null;

            Context.VersionDefinition = BuildGlobalSettings.Instance.VersionDefinition;
            Context.VersionDefinition.IncrementCurrentVersion();
            Context.VersionDefinition.ApplyCurrentVersion();

            UserConfig.PathResolver.ResolvePath(Preset);
            _Context.Defines = Preset.GetDefines(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget).ToList();
            PlayerSettings.GetScriptingDefineSymbols(PlatformConfig.NamedBuildTarget, out _PrevDefines);
            
            DebugLite.Log("[BuildRunner] StateCapture Actions executing");
            var buildActions = Preset.GetBuildActions(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget).ToList();
            yield return ExecuteBuildActions(buildActions, buildAction => buildAction.OnStateCapture(_Context));
            DebugLite.Log("[BuildRunner] StateCapture Actions completed");

            DebugLite.Log("[BuildRunner] Applying settings");
            SetDefineSymbols(PlatformConfig.NamedBuildTarget, _Context.Defines);

            _PrevBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            _PrevWasServer = BuildUtils.CurrentNamedBuildTarget == NamedBuildTarget.Server;
            EditorUserBuildSettings.SwitchActiveBuildTarget(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget);

            CompilationPipeline.RequestScriptCompilation();
        }

        private IEnumerator PreprocessBuild()
        {
            Context.State = BuildStateType.Preprocess;

            SetupSettingsBeforeBuild();
            DebugLite.Log("[BuildRunner] Preprocess Actions executing");
            var buildActions = Preset.GetBuildActions(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget).ToList();
            yield return ExecuteBuildActions(buildActions, buildAction => buildAction.OnPreprocess(_Context));
            DebugLite.Log("[BuildRunner] Preprocess Actions completed");
            
            PathUtility.EnsurePathExists(UserConfig.PathResolver.FullPath);
        }

        private IEnumerator BuildBinary()
        {
            yield return null;
            
            Context.State = BuildStateType.PipelinePreparation;
            DebugLite.Log($"[BuildRunner] Starting build (Path: {UserConfig.PathResolver.FullPath})");

            try
            {
                Context.State = BuildStateType.PipelineInProgress;
                _Context.Report = BuildPipeline.BuildPlayer(PlatformConfig.SceneList, UserConfig.PathResolver.FullPath, PlatformConfig.BuildTarget, Context.Options);
                _Context.Result = _Context.Report.summary.result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _Context.Result = BuildResult.Failed;
            }
            finally
            {
                Context.State = BuildStateType.PipelineFinalization;
            }

            if (_Context.Result == BuildResult.Succeeded)
            {
                yield return ProcessSuccessfulBuild();
            }

            DebugLite.Log("[BuildRunner] Build Completed");
        }

        private IEnumerator PostProcessAndChangeEditor()
        {
            Context.State = BuildStateType.Postprocess;

            DebugLite.Log("[BuildRunner] Postprocess Actions executing");
            var buildActions = Preset.GetBuildActions(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget).ToList();
            yield return ExecuteBuildActions(buildActions, buildAction => buildAction.OnPostprocess(_Context));
            DebugLite.Log("[BuildRunner] Postprocess Actions completed");

            DebugLite.Log("[BuildRunner] Restoring settings");
            SetDefineSymbols(PlatformConfig.NamedBuildTarget, _PrevDefines);
            var prevNamedBuildTarget =_PrevWasServer ? NamedBuildTarget.Server : NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(_PrevBuildTarget));
            EditorUserBuildSettings.SwitchActiveBuildTarget(prevNamedBuildTarget, _PrevBuildTarget);

            CompilationPipeline.RequestScriptCompilation();
        }
        
        private IEnumerator RestoreEditorState()
        {
            Context.State = BuildStateType.StateRestore;

            RestoreSettingsAfterBuild();

            DebugLite.Log("[BuildRunner] Actions pre defines executing");
            var buildActions = Preset.GetBuildActions(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget).ToList();
            yield return ExecuteBuildActions(buildActions, buildAction => buildAction.OnStateRestore(_Context));
            DebugLite.Log("[BuildRunner] Actions pre defines completed");

            DebugLite.Log("[BuildRunner] Build Postprocess Completed");
            Context.State = BuildStateType.Completed;

            if (_Context.Result != BuildResult.Unknown)
            {
                if (_Context.Result == BuildResult.Succeeded)
                    DebugLite.Log($"[BuildRunner] {PlatformConfig.NamedBuildTarget} Build completed");
                else
                    Debug.LogError($"[BuildRunner] {PlatformConfig.NamedBuildTarget} Build result: '{_Context.Report.summary.result}'");
            }

            if (Application.isBatchMode)
                EditorApplication.Exit(_Context.Result == BuildResult.Succeeded ? 0 : 1);
        }

        private void SetDefineSymbols(NamedBuildTarget namedTarget, IEnumerable<string> defines)
        {
            DebugLite.Log($"[BuildHelper] Previous defines: {PlayerSettings.GetScriptingDefineSymbols(namedTarget)}");
            var definesArray = defines.ToArray();
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, definesArray);
            AssetDatabase.SaveAssets();
            DebugLite.Log($"[BuildHelper] New defines: {string.Join(",", definesArray)}");
        }

        private static IEnumerator ExecuteBuildActions(IEnumerable<ABuildAction> buildActions, Func<ABuildAction, IEnumerator> function)
        {
            foreach (var buildAction in buildActions)
            {
                if (buildAction == null)
                    continue;

                DebugLite.Log($"[BuildRunner] Executing build action {buildAction.Label}");
                yield return function(buildAction);
            }
        }

        protected virtual IEnumerator ProcessSuccessfulBuild()
        {
            yield break;
        }

        protected virtual void SetupSettingsBeforeBuild()
        {
            _PrevStripEngineCode = PlayerSettings.stripEngineCode;
            PlayerSettings.stripEngineCode = PlatformConfig.StripEngineCode;

            _PrevManagedStrippingLevel = PlayerSettings.GetManagedStrippingLevel(PlatformConfig.NamedBuildTarget);
            PlayerSettings.SetManagedStrippingLevel(PlatformConfig.NamedBuildTarget, PlatformConfig.ManagedStrippingLevel);

            _PrevLogs = LoggingSetup.GetCurrent();
            PlatformConfig.LoggingSetup.Apply();

            _PrevWaitForManagedDebugger = EditorUserBuildSettings.waitForManagedDebugger;
            EditorUserBuildSettings.waitForManagedDebugger = PlatformConfig.WaitForManagedDebugger;

            _PrevEnableDeepProfile = EditorUserBuildSettings.buildWithDeepProfilingSupport;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = PlatformConfig.EnableDeepProfile;

            _PrevAutoConnectProfiler = EditorUserBuildSettings.connectProfiler;
            EditorUserBuildSettings.connectProfiler = PlatformConfig.AutoConnectProfiler;

            _PrevGCIncremental = PlayerSettings.gcIncremental;
            PlayerSettings.gcIncremental = PlatformConfig.UseIncrementalGC;

            if (PlatformConfig.DevelopmentBuild) Context.Options |= BuildOptions.Development;
            if (PlatformConfig.AllowDebugging) Context.Options |= BuildOptions.AllowDebugging;
            if (PlatformConfig.CleanBuildCache) Context.Options |= BuildOptions.CleanBuildCache;
            if (PlatformConfig.DetailedBuildReport) Context.Options |= BuildOptions.DetailedBuildReport;
            Context.Options |= PlatformConfig.PlayerCompression.ToBuildOptions();
        }

        protected virtual void RestoreSettingsAfterBuild()
        {
            PlayerSettings.SetManagedStrippingLevel(PlatformConfig.NamedBuildTarget, _PrevManagedStrippingLevel);
            EditorUserBuildSettings.waitForManagedDebugger = _PrevWaitForManagedDebugger;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = _PrevEnableDeepProfile;
            EditorUserBuildSettings.connectProfiler = _PrevAutoConnectProfiler;
            PlayerSettings.gcIncremental = _PrevGCIncremental;
            PlayerSettings.stripEngineCode = _PrevStripEngineCode;
            _PrevLogs.Apply();
        }
    }
}
