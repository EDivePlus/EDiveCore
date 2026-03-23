// Author: František Holubec
// Created: 19.03.2026

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace EDIVE.BuildTool.XRManagement
{
    [Serializable]
    public class XRPlatformModule : APlatformModule, IStateCaptureBuildCallback, IStateRestoreBuildCallback
    {
        public override string Label => "XR Management";

        [EnhancedBoxGroup("XR Management", "@ColorTools.Purple")]
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedValueDropdown(nameof(GetLoadersDropdown), DrawDropdownForListElements = false, IsUniqueList = true, IconGetter = nameof(GetLoaderIcon))]
        [EnhancedValidate(nameof(ValidateLoaders))]
        [EnhancedValidate(nameof(ValidateLoader), ApplyToListElements = true)]
        private List<XRLoader> _Loaders = new();
        
        private IEnumerable<XRLoader> GetValidLoaders(BuildTargetGroup group) => _Loaders.Where(l => l != null && l.IsSupportedBy(group));
        
        
        public IEnumerator OnStateCapture(BuildContext context)
        {
            var data = context.GetOrCreateData<Data>();
            
            var buildTargetGroup = context.PlatformConfig.BuildTargetGroup;
            var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            var pluginsSettings = buildTargetSettings.AssignedSettings;

            data._PrevLoaders = pluginsSettings.activeLoaders.ToList();
            data._PrevLoaders.ForEach(l => pluginsSettings.TryRemoveLoader(l));
            GetValidLoaders(buildTargetGroup).ForEach(l => pluginsSettings.TryAddLoader(l));
            
            EditorUtility.SetDirty(pluginsSettings);
            AssetDatabase.SaveAssets();
            yield break;
        }

        public IEnumerator OnStateRestore(BuildContext context)
        {
            if (!context.TryGetData<Data>(out var data))
                yield break;
            
            var buildTargetGroup = context.PlatformConfig.BuildTargetGroup;
            var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            var pluginsSettings = buildTargetSettings.AssignedSettings;
            
            pluginsSettings.activeLoaders.ToList().ForEach(l => pluginsSettings.TryRemoveLoader(l));
            data._PrevLoaders.ForEach(l => pluginsSettings.TryAddLoader(l));
        }
        
        [Serializable]
        private class Data : ABuildContextData
        {
            [SerializeField]
            public List<XRLoader> _PrevLoaders;
        }

        private Texture2D GetLoaderIcon() => EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;

        private IEnumerable GetLoadersDropdown(InspectorProperty property)
        {
            if (!TryGetModuleFromProperty(property, out ABuildTargetPlatformModule module))
                return Enumerable.Empty<XRLoader>();

            return GetAllAvailableLoaders(module.BuildTargetGroup)
                .Select(l => new ValueDropdownItem<XRLoader>(l.name, l));
        }
        
        private static IEnumerable<XRLoader> GetAllAvailableLoaders(BuildTargetGroup buildTargetGroup)
        {
            return EditorAssetUtils.FindAllAssetsOfType<XRLoader>().Where(l => l.IsSupportedBy(buildTargetGroup));
        }

        private void ValidateLoader(XRLoader value, SelfValidationResult result, InspectorProperty property)
        {
            if (!TryGetModuleFromProperty(property, out ABuildTargetPlatformModule module))
                return;

            var incompatibleLoaders = value.SelectIncompatibleLoaders(_Loaders, module.BuildTargetGroup).ToList();
            if (incompatibleLoaders.Any())
            {
                result.AddError($"Incompatible with: {string.Join(", ", incompatibleLoaders.Select(l => l.name))}.");
            }
        }
        
        private void ValidateLoaders(List<XRLoader> value, SelfValidationResult result, InspectorProperty property)
        {
            if (!TryGetModuleFromProperty(property, out ABuildTargetPlatformModule module))
                return;

            var invalidLoaders = value.Where(l => l == null || !l.IsSupportedBy(module.BuildTargetGroup)).ToList();
            if (invalidLoaders.Any())
            {
                result.AddError($"Contains incompatible loaders: {string.Join(", ", invalidLoaders.Select(l => l?.name ?? "null"))}.")
                    .WithFix(() =>
                    {
                        value.RemoveAll(l => l == null || !l.IsSupportedBy(module.BuildTargetGroup));
                        property.MarkSerializationRootDirty();
                    });
            }
        }
    }
}
