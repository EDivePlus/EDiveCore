// Author: František Holubec
// Created: 19.03.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;

namespace EDIVE.BuildTool.XRManagement
{
    public static class XRManagementUtility
    {
        public class CustomLoaderUiInfo
        {
            public string LoaderTypeName { get; }
            public BuildTargetGroup BuildTargetGroup { get; }
            public string[] IncompatibleLoaders { get; }

            public CustomLoaderUiInfo(string loaderTypeName, BuildTargetGroup buildTargetGroup, string[] incompatibleLoaders)
            {
                LoaderTypeName = loaderTypeName;
                BuildTargetGroup = buildTargetGroup;
                IncompatibleLoaders = incompatibleLoaders;
            }
        }

        private static List<CustomLoaderUiInfo> _customLoaderUiInfos;
        
        public static bool TryGetCustomLoaderUiInfo(string loaderTypeName, BuildTargetGroup buildTargetGroup, out CustomLoaderUiInfo loaderUiInfo)
        {
            if (_customLoaderUiInfos == null)
            {
                _customLoaderUiInfos = new List<CustomLoaderUiInfo>();
                var loaderUis = TypeCacheUtils.GetDerivedClassesOfType<IXRCustomLoaderUI>();
                foreach (var loaderUi in loaderUis)
                {
                    var attributes = loaderUi.GetType().GetCustomAttributes(typeof(XRCustomLoaderUIAttribute), false)
                        .Cast<XRCustomLoaderUIAttribute>();
                    foreach (var attribute in attributes)
                    {
                        _customLoaderUiInfos.Add(new CustomLoaderUiInfo(attribute.loaderTypeName, attribute.buildTargetGroup, loaderUi.IncompatibleLoaders));
                    }
                }
            }

            return _customLoaderUiInfos.TryGetFirst(info => info.LoaderTypeName == loaderTypeName && info.BuildTargetGroup == buildTargetGroup, out loaderUiInfo);
        }

        public static (IXRLoaderMetadata loaderMeta, IXRPackageMetadata packageMeta) GetLoaderMetadata(this XRLoader loader)
        {
            if (loader == null)
                return default;
            
            var loaderTypeName = loader.GetType().FullName;
            foreach (var packageData in XRPackageMetadataStore.GetAllPackageMetadata())
            {
                foreach (var loaderMeta in packageData.metadata.loaderMetadata)
                {
                    if (loaderMeta.loaderType != loaderTypeName)
                        continue;

                    return (loaderMeta, packageData.metadata);
                }
            }
            return default;
        }
        
        public static bool IsSupportedBy(this XRLoader loader, BuildTargetGroup buildTargetGroup)
        {
            var metadata = loader.GetLoaderMetadata();
            return metadata != default && metadata.loaderMeta.supportedBuildTargets.Contains(buildTargetGroup);
        }
        
        public static IEnumerable<(IXRLoaderMetadata loader, IXRPackageMetadata package)> GetAllLoadersMetadata(BuildTargetGroup buildTargetGroup)
        {
            var allPackages = XRPackageMetadataStore.GetAllPackageMetadataForBuildTarget(buildTargetGroup);
            foreach (var package in allPackages)
            {
                foreach (var loader in package.metadata.loaderMetadata)
                {
                    yield return (loader, package.metadata);
                }
            }
        }
    }
}
