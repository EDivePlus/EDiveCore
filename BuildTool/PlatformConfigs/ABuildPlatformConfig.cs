using EDIVE.BuildTool.Utils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.PlatformConfigs
{
    public abstract class ABuildPlatformConfig : ScriptableObject
    {
        [EnhancedBoxGroup("Build", "@Color.cyan", SpaceBefore = 6)]
        [SerializeField]
        private bool _DevelopmentBuild;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _AllowDebugging;

        [EnhancedBoxGroup("Build")]
        [EnableIf(nameof(_DevelopmentBuild))]
        [SerializeField]
        private bool _BuildScriptsOnly;

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

        [EnhancedBoxGroup("Stripping", "@Color.red", SpaceBefore = 6)]
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

        [EnhancedBoxGroup("Logging","@Color.yellow", SpaceBefore = 6)]
        [SerializeField]
        [HideLabel]
        [InlineProperty]
        private LoggingSetup _LoggingSetup;

        [EnhancedBoxGroup("Path", "@Color.green", SpaceBefore = 6)]
        [SerializeField]
        private string _PlatformName;

        [EnhancedBoxGroup("Path")]
        [SerializeField]
        private string _ConfigID;

        [PropertySpace]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private BuildSetupData _BuildSetupData;

        [SerializeField]
        private SceneListDefinition _SceneList;

        public bool DevelopmentBuild => _DevelopmentBuild;
        public bool AllowDebugging => _AllowDebugging;
        public bool BuildScriptsOnly => _BuildScriptsOnly;
        public bool CleanBuildCache => _CleanBuildCache;
        public bool WaitForManagedDebugger => _WaitForManagedDebugger;
        public bool EnableDeepProfile => _EnableDeepProfile;
        public bool AutoConnectProfiler => _AutoConnectProfiler;
        public bool DetailedBuildReport => _DetailedBuildReport;
        public string PlatformName => _PlatformName;
        public string ConfigID => _ConfigID;
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

        [PropertySpace(6)]
        [PropertyOrder(100)]
        [Button(ButtonStyle.CompactBox, Expanded = true)]
        private void CopyValuesFrom(ABuildPlatformConfig buildPlatformConfig)
        {
            if (buildPlatformConfig == null)
                return;

            EditorUtility.CopySerialized(buildPlatformConfig, this);
        }
    }
}
