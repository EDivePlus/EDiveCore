using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.BuildSetupData;
using EDIVE.BuildTool.Utils;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.PlatformConfigs
{
    public class BuildPlatformConfig : ScriptableObject, IBuildSetupDataProvider
    {
        [Required]
        [LabelText("Target Platform")]
        [SerializeReference]
        [EnhancedValueDropdown(nameof(GetAvailableBaseModulesDropdown), SpaceBeforeChildren = 4)]
        internal ABasePlatformModule _BaseModule;
        public ABasePlatformModule BaseModule => _BaseModule;
        
        [PropertySpace(4)]
        [EnhancedValueDropdown(nameof(GetAvailableModulesDropdown), DrawDropdownForListElements = false, IsUniqueList =  true)]
        [CustomValueDrawer(nameof(CustomModuleDrawer))]
        [ListDrawerSettings(ShowFoldout = false, HideRemoveButton = true, DraggableItems = false, OnTitleBarGUI = nameof(OnModulesListTitleBarGUI))]
        [OnCollectionChanged(nameof(OnModulesCollectionChanged))]
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
        
        public bool IsValid => BaseModule != null;
        public SerializedBuildSetupData BuildSetupData => _BuildSetupData;
        public SceneListDefinition OverrideSceneList => _OverrideSceneList;

        public IEnumerable<APlatformModule> ModulesOrdered => GetAllModules().Where(m => m != null).OrderBy(m => m.ExecutionOrder);
        
        private IEnumerable<APlatformModule> GetAllModules()
        {
            if(_BaseModule != null)
                yield return _BaseModule;
            if (_AdditionalModules == null) 
                yield break;
            foreach (var module in _AdditionalModules)
                yield return module;
        }

        public bool TryGetModule<T>(out T module) where T : APlatformModule
        {
            return ModulesOrdered.TryGetFirstT(out module);
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

        private IEnumerable GetAvailableModulesDropdown()
        {
            return GetAvailableModules().Select(m => new ValueDropdownItem<APlatformModule>(m.Label, m));
        }
        
        private IEnumerable<APlatformModule> GetAvailableModules()
        {
            return BaseModule != null 
                ? TypeCacheUtils.GetAssignableClassesOfType<APlatformModule>()
                    .Where(m => m is not ABasePlatformModule  && m.SupportsTarget(BaseModule.BuildTarget)) 
                : Enumerable.Empty<APlatformModule>();
        }
        
        private IEnumerable GetAvailableBaseModulesDropdown()
        {
            return TypeCacheUtils.GetAssignableClassesOfType<ABasePlatformModule>().Select(m => new ValueDropdownItem<APlatformModule>(m.Label, m));
        }
        
        private void CustomModuleDrawer(APlatformModule value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            EditorGUILayout.LabelField(GUIHelper.TempContent(value.Label), EditorStyles.boldLabel);
            callNextDrawer?.Invoke(label);
        }

        private void OnModulesCollectionChanged()
        {
            _AdditionalModules.Sort();
        }
        
        private void OnModulesListTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                _AdditionalModules.RemoveAll(m => (BaseModule != null && !m.SupportsTarget(BaseModule.BuildTarget)) || m is null or ABasePlatformModule);
                foreach (var module in GetAvailableModules())
                {
                    if (!_AdditionalModules.Contains(module)) 
                        _AdditionalModules.Add(module);
                }
                _AdditionalModules.Sort();
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
