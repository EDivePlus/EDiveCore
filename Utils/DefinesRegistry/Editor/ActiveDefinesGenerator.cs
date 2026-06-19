// Author: František Holubec
// Created: 07.05.2026

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using EDIVE.EditorUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace EDIVE.Utils.DefinesRegistry
{
    public class ActiveDefinesGenerator : IPreprocessBuildWithReport
    {
        private const string DEFAULT_ASSET_DIR = "Assets/Resources";
        private const string DEFAULT_ASSET_PATH = DEFAULT_ASSET_DIR + "/" + ActiveDefinesRegistry.RESOURCE_PATH + ".asset";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Regenerate();

        private static void Regenerate()
        {
            var defines = CollectDefines();
            var asset = TryFindExistingAsset(out var foundAsset) ? foundAsset : CreateAssetAtDefaultPath(defines);

            if (asset.Defines.SequenceEqual(defines))
                return;

            asset.SetDefines(defines);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            ActiveDefinesRegistry.Invalidate();
        }

        private static bool TryFindExistingAsset(out ActiveDefinesAsset asset)
        {
            var assets = EditorAssetUtils.FindAllAssetsOfType<ActiveDefinesAsset>();
            if (assets.Count == 0)
            {
                asset = null;
                return false;
            }
            asset = assets[0];
            return true;
        }

        private static ActiveDefinesAsset CreateAssetAtDefaultPath(string[] defines)
        {
            if (!Directory.Exists(DEFAULT_ASSET_DIR))
                Directory.CreateDirectory(DEFAULT_ASSET_DIR);

            var asset = ScriptableObject.CreateInstance<ActiveDefinesAsset>();
            asset.SetDefines(defines);
            AssetDatabase.CreateAsset(asset, DEFAULT_ASSET_PATH);
            return asset;
        }

        private static string[] CollectDefines()
        {
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            return assemblies
                .SelectMany(a => a.defines ?? Array.Empty<string>())
                .Distinct()
                .OrderBy(d => d, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
#endif
