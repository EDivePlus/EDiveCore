using EDIVE.BuildTool.Presets;
using EDIVE.BuildTool.Utils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.BuildTool.PlatformConfigs
{
    public abstract class ABuildPlatformConfig : ScriptableObject
    {
        [EnhancedBoxGroup("Build", "@ColorTools.Cyan", SpaceBefore = 6)]
        [SerializeField]
        private bool _DevelopmentBuild;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _AllowDebugging;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _WaitForManagedDebugger;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _AutoConnectProfiler;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _EnableDeepProfile;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _CleanBuildCache;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _DetailedBuildReport;

        [EnhancedBoxGroup("Stripping", "@ColorTools.Red", SpaceBefore = 6)]
        [SerializeField]
        private bool _StripEngineCode = true;

        [EnhancedBoxGroup("Stripping")]
        [SerializeField]
        private ManagedStrippingLevel _ManagedStrippingLevel = ManagedStrippingLevel.Low;

        [EnhancedBoxGroup("Stripping")]
        [SerializeField]
        private PlayerCompressionType _PlayerCompression = PlayerCompressionType.Default;

        [EnhancedBoxGroup("Stripping")]
        [SerializeField]
        private bool _UseIncrementalGC = true;

        [EnhancedBoxGroup("Logging","@ColorTools.Yellow", SpaceBefore = 6)]
        [SerializeField]
        [HideLabel]
        [InlineProperty]
        private LoggingSetup _LoggingSetup;

        [EnhancedBoxGroup("Path", "@ColorTools.Green", SpaceBefore = 6)]
        [SerializeField]
        private string _PlatformName;

        [EnhancedBoxGroup("Path")]
        [SerializeField]
        private string _ConfigType;

        [EnhancedBoxGroup("Data", "@ColorTools.Lime", SpaceBefore = 6)]
        [SerializeField]
        private SceneListDefinition _SceneList;

        [EnhancedBoxGroup("Data")]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private BuildSetupData _BuildSetupData;

        public bool DevelopmentBuild => _DevelopmentBuild;
        public bool AllowDebugging => _AllowDebugging;
        public bool CleanBuildCache => _CleanBuildCache;
        public bool WaitForManagedDebugger => _WaitForManagedDebugger;
        public bool EnableDeepProfile => _EnableDeepProfile;
        public bool AutoConnectProfiler => _AutoConnectProfiler;
        public bool DetailedBuildReport => _DetailedBuildReport;
        public string PlatformName => _PlatformName;
        public string ConfigType => _ConfigType;
        public bool StripEngineCode => _StripEngineCode;
        public ManagedStrippingLevel ManagedStrippingLevel => _ManagedStrippingLevel;
        public PlayerCompressionType PlayerCompression => _PlayerCompression;
        public bool UseIncrementalGC => _UseIncrementalGC;
        public LoggingSetup LoggingSetup => _LoggingSetup;
        public BuildSetupData BuildSetupData => _BuildSetupData;
        public SceneListDefinition SceneList => _SceneList;

        public abstract NamedBuildTarget NamedBuildTarget { get; }
        public abstract BuildTarget BuildTarget { get; }
        public abstract string BuildExtension { get; }

        public abstract ABuildPreset CreatePreset(BuildUserConfig userConfig);

        [PropertySpace(6)]
        [PropertyOrder(100)]
        [Button]
        private void CopyValuesFrom(ABuildPlatformConfig buildPlatformConfig)
        {
            if (buildPlatformConfig == null)
                return;

            EditorUtility.CopySerialized(buildPlatformConfig, this);
        }
    }
}
