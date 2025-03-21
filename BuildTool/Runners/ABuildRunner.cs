using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.BuildTool.Actions;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Presets;
using EDIVE.BuildTool.Utils;
using EDIVE.EditorUtils.DomainReload;
using EDIVE.NativeUtils;
using EDIVE.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

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

        public BuildContext Context => _Context;

        public abstract UniTask StartBuild();
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

        public override async UniTask StartBuild()
        {
            DebugLite.Log("[BuildRunner] Build prepared for start or resume...");

            await UniTask.Yield();
            await UniTask.WaitWhile(() => EditorApplication.isCompiling);

            if (Context.State != BuildStateType.NotStarted)
            {
                DebugLite.Log($"[BuildRunner] Resuming build from state {Context.State}");
            }

            try
            {
                if (Context.State == BuildStateType.NotStarted)
                {
                    await ExecuteBuildSegment(BuildPreprocess);
                }

                if (Context.State < BuildStateType.PipelinePreparation)
                {
                    await ExecuteBuildSegment(BuildBinary);
                }

                if (Context.State < BuildStateType.Postprocessing)
                {
                    await ExecuteBuildSegment(BuildPostprocess);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                TeamCityServiceMessages.MessageBuildProblem("Build failed with exception");
                _Context.Result = BuildResult.Failed;
                RestoreSettingsAfterBuild();
            }
        }

        private async UniTask ExecuteBuildSegment(Func<UniTask> segmentFunction)
        {
            DebugLite.Log($"[BuildRunner] Processing state {Context.State}");
            EditorApplication.LockReloadAssemblies();
            await segmentFunction();
            DomainReloadUtility.RegisterSurvivor(DOMAIN_RELOAD_SURVIVOR_ID, new BuildRunnerDomainReloadSurvivor(this));
            EditorApplication.UnlockReloadAssemblies();
            await UniTask.Yield();
            await UniTask.WaitWhile(() => EditorApplication.isCompiling);
            DomainReloadUtility.ClearSurvivor(DOMAIN_RELOAD_SURVIVOR_ID);
        }

        public async UniTask BuildPreprocess()
        {
            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Build Preprocess");
            Context.State = BuildStateType.Preprocessing;
            Debug.Log("[BuildRunner] Build preprocess started");

            Preset.Validate();
            await UniTask.Yield();

            UserConfig.PathResolver.ResolvePath(Preset);

            if (EditorUserBuildSettings.activeBuildTarget != PlatformConfig.BuildTarget)
                EditorUserBuildSettings.SwitchActiveBuildTarget(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget);

            SetupSettingsBeforeBuild();

            _Context.Defines = Preset.GetDefines(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget).ToList();
            var buildActions = Preset.GetBuildActions(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget).ToList();

            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Build Setup");

            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Actions Before DEFINES");
            Debug.Log("[BuildRunner] Executing Build actions before defines ...");
            await ExecuteBuildActions(buildActions, buildAction => buildAction.OnPreBuildBeforeDefines(_Context));
            Debug.Log("[BuildRunner] PreBuildCallbacks before defines executed ");
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Actions Before DEFINES");

            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Applying DEFINES");
            Debug.Log("[BuildRunner] Begin Apply DEFINES");
            PlayerSettings.GetScriptingDefineSymbols(PlatformConfig.NamedBuildTarget, out _PrevDefines);
            SetDefineSymbols(PlatformConfig.NamedBuildTarget, _Context.Defines);
            await BuildUtils.RequestAndAwaitCompilation();
            Debug.Log("[BuildRunner] End apply DEFINES");
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Applying DEFINES");

            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Actions After DEFINES");
            Debug.Log("[BuildRunner] Begin actions pre-build after defines");
            await ExecuteBuildActions(buildActions, buildAction => buildAction.OnPreBuildAfterDefines(_Context));
            Debug.Log("[BuildRunner] End actions pre-build after defines");
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Actions After DEFINES");

            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Build Preprocess");
        }

        public async UniTask BuildBinary()
        {
            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Build Binary");
            Context.State = BuildStateType.PipelinePreparation;
            Debug.Log("[BuildRunner] Starting build");
            await UniTask.Yield();

            await PreprocessBeforeBuildPipeline();
            Debug.Log($"[BuildRunner] Build path: {UserConfig.PathResolver.FullPath}");

            try
            {
                Context.State = BuildStateType.PipelineInProgress;
                _Context.Report = BuildPipeline.BuildPlayer(PlatformConfig.SceneList, UserConfig.PathResolver.FullPath, PlatformConfig.BuildTarget, Context.Options);
                _Context.Result = _Context.Report.summary.result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                TeamCityServiceMessages.MessageBuildProblem("Build pipeline failed with exception");
                _Context.Result = BuildResult.Failed;
            }
            finally
            {
                Context.State = BuildStateType.PipelineFinalization;
            }

            Debug.Log("[BuildRunner] Build Completed");
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Build Binary");
        }

        public async UniTask BuildPostprocess()
        {
            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Build Postprocess");
            Context.State = BuildStateType.Postprocessing;

            var buildActions = Preset.GetBuildActions(PlatformConfig.NamedBuildTarget, PlatformConfig.BuildTarget).ToList();

            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Restoration After Build");
            RestoreSettingsAfterBuild();
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Restoration After Build");

            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Actions Before Reverting DEFINES");
            Debug.Log("[BuildRunner] Executing PostBuildCallbacks before defines ...");
            await ExecuteBuildActions(buildActions, buildAction => buildAction.OnPostBuildBeforeDefines(_Context));
            Debug.Log("[BuildRunner] PostBuildCallbacks before defines executed ");
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Actions Before Reverting DEFINES");

            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Reverting DEFINES");
            SetDefineSymbols(PlatformConfig.NamedBuildTarget, _PrevDefines);
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Reverting DEFINES");

            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Actions After Reverting DEFINES");
            Debug.Log("[BuildRunner] Executing PostBuildCallbacks after defines ...");
            await ExecuteBuildActions(buildActions, buildAction => buildAction.OnPostBuildAfterDefines(_Context));
            Debug.Log("[BuildRunner] PostBuildCallbacks after defines executed ");
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Actions After Reverting DEFINES");

            TeamCityServiceMessages.BeginMessageBlock("[BuildRunner] Finalization");
            if (_Context.Report != null)
            {
                _Context.Result = _Context.Report.summary.result;
                if (_Context.Report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"[BuildRunner] {PlatformConfig.NamedBuildTarget} build completed");
                    await ProcessSuccessfulBuild();
                }
                else
                {
                    Debug.LogError($"[BuildRunner] {PlatformConfig.NamedBuildTarget} build result: '{_Context.Report.summary.result}'");
                }
            }

            Debug.Log("[BuildRunner] Build Postprocess Completed");
            Context.State = BuildStateType.Completed;
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Finalization");
            TeamCityServiceMessages.EndMessageBlock("[BuildRunner] Build Postprocess");

            if (Application.isBatchMode)
                EditorApplication.Exit(_Context.Result == BuildResult.Succeeded ? 0 : 1);
        }

        public void SetDefineSymbols(NamedBuildTarget namedTarget, IEnumerable<string> defines)
        {
            Debug.Log($"[BuildHelper] Previous defines: {PlayerSettings.GetScriptingDefineSymbols(namedTarget)}");
            var definesArray = defines.ToArray();
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, definesArray);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildHelper] New defines: {string.Join(",", definesArray)}");
        }

        protected virtual UniTask PreprocessBeforeBuildPipeline()
        {
            PathUtility.EnsurePathExists(UserConfig.PathResolver.FullPath);
            return UniTask.CompletedTask;
        }

        private static async UniTask ExecuteBuildActions(IEnumerable<ABuildAction> buildActions, Func<ABuildAction, UniTask> taskGetter)
        {
            foreach (var buildAction in buildActions)
            {
                if (buildAction == null)
                    continue;

                Debug.Log($"[BuildRunner] Executing build action {buildAction.Label}");
                await taskGetter(buildAction);
            }
        }

        protected virtual UniTask ProcessSuccessfulBuild()
        {
            return UniTask.CompletedTask;
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
