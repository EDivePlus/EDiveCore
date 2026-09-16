// Author: František Holubec
// Created: 20.03.2025

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.BuildSetupData;
using EDIVE.BuildTool.PathResolving;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.BuildTool.UserConfigs
{
    public class BuildUserConfig : ScriptableObject, IBuildDataProvider
    {
        [EnhancedBoxGroup("Path Resolver", "@ColorTools.Yellow")]
        [InlineProperty]
        [HideLabel]
        [SerializeField]
        private BuildPathResolver _PathResolver;

        [PropertySpace(4)]
        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedValueDropdown(nameof(GetPreferencesDropdown), IsUniqueList = true, DrawDropdownForListElements = false, CustomComparer = nameof(CustomUserPreferenceComparer))]
        private List<AUserPreference> _Preferences = new();

        [PropertySpace(4)] 
        [SerializeField]
        [InlineProperty]
        [HideLabel]
        private MultiPlatformBuildSetupData _BuildSetupData;

        public BuildPathResolver PathResolver => _PathResolver;

        public bool TryGetPreference<T>(out T preference) where T : AUserPreference
        {
            preference = null;
            return _Preferences != null && _Preferences.TryGetFirstT(out preference);
        }

        public T GetOrCreatePreference<T>() where T : AUserPreference, new()
        {
            if (TryGetPreference<T>(out var preference))
                return preference;

            _Preferences ??= new List<AUserPreference>();
            preference = new T();
            _Preferences.Add(preference);
            return preference;
        }

        public IEnumerable<string> GetBuildDefines(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget)
                .SelectMany(d => d.Defines);
        }

        public IEnumerable<string> GetBuildScenes(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget)
                .SelectMany(d => d.Scenes);
        }
        
        public IEnumerable<IBuildCallback> GetBuildCallbacks(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget)
                .SelectMany(d => d.Actions);
        }

        private IEnumerable<ValueDropdownItem<AUserPreference>> GetPreferencesDropdown()
        {
            return TypeCacheUtils.GetAssignableClassesOfType<AUserPreference>()
                .Select(p => new ValueDropdownItem<AUserPreference>(p.Label, p));
        }
        
        private IEqualityComparer CustomUserPreferenceComparer => TypeEqualityComparer<AUserPreference>.INSTANCE;
    }
}
