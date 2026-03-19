using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.BuildSetupData;
using EDIVE.BuildTool.Utils;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.PlatformConfigs
{
    public class BuildPlatformConfig : ScriptableObject, IBuildSetupDataProvider
    {
        [Required]
        [LabelText("Build Target Group")]
        [SerializeReference]
        [EnhancedValueDropdown(nameof(GetBuildTargetModulesDropdown), SpaceBeforeChildren = 4, ValueLabelGetter = nameof(GetBuildTargetModuleLabel))]
        internal ABuildTargetPlatformModule _BuildTargetModule;
        public ABuildTargetPlatformModule BuildTargetModule => _BuildTargetModule;
        
        [PropertySpace(4)]
        [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true, OnTitleBarGUI = nameof(OnModulesListTitleBarGUI))]
        [EnhancedValidate(nameof(ValidateAdditionalModules))]
        [HideReferenceObjectPicker]
        [SerializeReference]
        internal List<APlatformModule> _AdditionalModules = new();
        
        [EnhancedBoxGroup("Data", "@ColorTools.Lime", SpaceBefore = 6)]
        [Tooltip("Override the scenes defined in the EditorBuildSettings with this Scene List")]
        [ShowCreateNew]
        [SerializeField]
        protected SceneListDefinition _OverrideSceneList;

        [EnhancedBoxGroup("Data")]
        [PropertySpace(4)]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        protected SerializedBuildSetupData _BuildSetupData;
        
        private static TypeEqualityComparer<APlatformModule> CustomModuleComparer => TypeEqualityComparer<APlatformModule>.INSTANCE;
        
        public bool IsValid => BuildTargetModule != null;
        public SerializedBuildSetupData BuildSetupData => _BuildSetupData;
        public SceneListDefinition OverrideSceneList => _OverrideSceneList;
        
        public IEnumerable<IPlatformModule> GetModules()
        {
            if(_BuildTargetModule != null)
                yield return _BuildTargetModule;
            if (_AdditionalModules == null) 
                yield break;
            foreach (var module in _AdditionalModules.Where(m => m != null).OrderBy(m => m.ExecutionOrder))
                yield return module;
        }

        public bool TryGetModule<T>(out T module) where T : IPlatformModule
        {
            return GetModules().TryGetFirstT(out module);
        }
        
        [PropertySpace(6)]
        [PropertyOrder(100)]
        [Button]
        private void CopyValuesFrom(BuildPlatformConfig buildPlatformConfig)
        {
            if (buildPlatformConfig == null)
                return;

            EditorUtility.CopySerialized(buildPlatformConfig, this);
        }

        public IEnumerable<IBuildSetupData> GetBuildSetupData(NamedBuildTarget namedTarget, BuildTarget target)
        {
            yield return BuildSetupData;
        }
        
        private IEnumerable<APlatformModule> GetAvailableModules()
        {
            return BuildTargetModule != null 
                ? TypeCacheUtils.GetAssignableClassesOfType<APlatformModule>()
                    .Where(m => m.SupportsTarget(BuildTargetModule.BuildTarget)) 
                : Enumerable.Empty<APlatformModule>();
        }
        
        private IEnumerable GetBuildTargetModulesDropdown()
        {
            return TypeCacheUtils.GetAssignableClassesOfType<ABuildTargetPlatformModule>()
                .Select(m => new ValueDropdownItem<ABuildTargetPlatformModule>(m.PlatformName, m));
        }
        
        private void OnModulesListTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                _AdditionalModules ??= new List<APlatformModule>();
                _AdditionalModules.RemoveAll(m => (BuildTargetModule != null && !m.SupportsTarget(BuildTargetModule.BuildTarget)) || m == null);
                foreach (var module in GetAvailableModules())
                {
                    if (!_AdditionalModules.Contains(module, CustomModuleComparer)) 
                        _AdditionalModules.Add(module);
                }
                _AdditionalModules.Sort();
            }
        }

        private string GetBuildTargetModuleLabel(ABuildTargetPlatformModule module) => module.PlatformName;
        
        private void ValidateAdditionalModules(SelfValidationResult result, InspectorProperty property)
        {
            if (_AdditionalModules == null || BuildTargetModule == null)
                return;
            
            if (_AdditionalModules.Any(m => m == null || !m.SupportsTarget(BuildTargetModule.BuildTarget)))
                result.AddError("Some modules are not supported for this target!")
                    .WithFix(() =>
                    {
                        _AdditionalModules.RemoveAll(m => m == null || !m.SupportsTarget(BuildTargetModule.BuildTarget));
                        property.MarkSerializationRootDirty();
                    });
            
            var missingModules = GetAvailableModules().Except(_AdditionalModules, CustomModuleComparer).ToList();
            if (missingModules.Any())
                result.AddError("Some available modules are not added to the list!")
                    .WithFix(() =>
                    {
                        _AdditionalModules.AddRange(missingModules);
                        _AdditionalModules.Sort();
                        property.MarkSerializationRootDirty();
                    });
            
            var duplicates = _AdditionalModules
                .Where(m => m != null)
                .GroupBy(m => m, CustomModuleComparer)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Skip(1)) // keep one, mark the rest as duplicates
                .ToList();

            if (duplicates.Any())
            {
                result.AddError("Duplicate modules detected!")
                    .WithFix(() =>
                    {
                        _AdditionalModules = _AdditionalModules
                            .Where(m => m != null)
                            .Distinct(CustomModuleComparer)
                            .ToList();
                        property.MarkSerializationRootDirty();
                    });
            }
        }

    #region Migration
        [SerializeField, HideInInspector] private bool _DevelopmentBuild;
        [SerializeField, HideInInspector] private bool _AllowDebugging;
        [SerializeField, HideInInspector] private bool _WaitForManagedDebugger;
        [SerializeField, HideInInspector] private bool _AutoConnectProfiler;
        [SerializeField, HideInInspector] private bool _EnableDeepProfile;
        [SerializeField, HideInInspector] private bool _CleanBuildCache;
        [SerializeField, HideInInspector] private bool _DetailedBuildReport;
        [SerializeField, HideInInspector] private bool _StripEngineCode = true;
        [SerializeField, HideInInspector] private ManagedStrippingLevel _ManagedStrippingLevel = ManagedStrippingLevel.Low;
        [SerializeField, HideInInspector] private PlayerCompressionType _PlayerCompression = PlayerCompressionType.Default;
        [SerializeField, HideInInspector] private bool _UseIncrementalGC = true;
        [SerializeField, HideInInspector] private LoggingSetup _LoggingSetup;
        [SerializeField, HideInInspector] private string _PlatformName;
        [SerializeField, HideInInspector] private string _ConfigType;
    #endregion
    }
}
