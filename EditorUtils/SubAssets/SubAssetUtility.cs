using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace EDIVE.EditorUtils.SubAssets
{
    public static class SubAssetUtility
    {
        public static void MoveAssets(IEnumerable<Object> sources, Object target)
        {
            var destinationPath = AssetDatabase.GetAssetPath(target);
            MoveAssets(sources, destinationPath);
        }
        
        public static void MoveAssets(IEnumerable<Object> sources, string destinationPath)
        {
            foreach (var source in sources)
            {
                var sourcePath = AssetDatabase.GetAssetPath(source);
                var sourceIsMain = AssetDatabase.IsMainAsset(source);
                var sourceAssets = new List<Object>() { source };
                if (sourceIsMain)
                    sourceAssets.AddRange(AssetDatabase.LoadAllAssetRepresentationsAtPath(sourcePath));

                // Perform move assets from source file to destination
                foreach (var asset in sourceAssets)
                    MoveAssetInternal(asset, destinationPath);

                // Remove asset file if it is empty now
                if (sourceIsMain)
                    AssetDatabase.DeleteAsset(sourcePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void MoveAssetInternal(Object asset, string destinationPath)
        {
            // Find hidden references (before source move)
            var assetRefs = GetHiddenReferences(asset);

            // Move asset
            var destinationIsFolder = AssetDatabase.IsValidFolder(destinationPath);
            if (destinationIsFolder)
            {
                var assetName = $"{asset.name}{GetFileExtension(asset)}";
                var assetPath = Path.Combine(destinationPath, assetName);
                var assetUniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                AssetDatabase.RemoveObjectFromAsset(asset);
                AssetDatabase.CreateAsset(asset, assetUniquePath);
            }
            else
            {
                AssetDatabase.RemoveObjectFromAsset(asset);
                AssetDatabase.AddObjectToAsset(asset, destinationPath);
            }
            
            // Move attached hidden references
            foreach (var reference in assetRefs)
            {
                AssetDatabase.RemoveObjectFromAsset(reference);
                AssetDatabase.AddObjectToAsset(reference, asset);
            }
        }

        public static void DeleteSubAsset(Object asset)
        {
            if (AssetDatabase.IsMainAsset(asset))
            {
                Debug.LogError("Target is not sub asset!");
                return;
            }
            Object.DestroyImmediate(asset, true);
            if (!Application.isPlaying)
            {
                AssetDatabase.ForceReserializeAssets(new[] {AssetDatabase.GetAssetPath(asset)});
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        
        public static void AddSubAsset(Object targetAsset, Object assetToAdd)
        {
            if (!AssetDatabase.IsMainAsset(targetAsset))
            {
                Debug.LogError("Target is not main asset!");
                return;
            }
            AssetDatabase.AddObjectToAsset(assetToAdd, targetAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static void ExportSubAsset(Object obj, string targetPath)
        {
            var exportName = $"{obj.name}{GetFileExtension(obj)}";
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, exportName));
            AssetDatabase.CreateAsset(Object.Instantiate(obj), uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void ExportSubAssetToCurrentFolder(Object obj)
        {
            var targetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(obj));
            ExportSubAsset(obj, targetPath);
        }
        
        public static IEnumerable<Object> GetAllSubAssets(Object obj)
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.LoadAllAssetsAtPath(assetPath);
        }

        public static bool IsSubAssetOf(Object obj, Object parentAsset)
        {
            return AssetDatabase.GetAssetPath(parentAsset) == AssetDatabase.GetAssetPath(obj);
        }

        public static T GetParentAsset<T>(Object obj) where T : Object
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
        
        public static IEnumerable<T> GetAllSubAssets<T>(Object obj)
        {
            return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj)).OfType<T>().Distinct();
        }
        
        public static void RenameSubAsset(Object obj, string newName)
        {
            AssetDatabase.ClearLabels(obj);
            obj.name = newName;
            AssetDatabase.SetLabels(obj, new []{newName});
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(obj));
        }
        
        private static List<Object> GetHiddenReferences(Object asset, string refsPath = null, List<Object> refs = null)
        {
            refsPath ??= AssetDatabase.GetAssetPath(asset);

            refs ??= new List<Object>();

            var iterator = new SerializedObject(asset).GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                var obj = iterator.objectReferenceValue;
                if (obj == null || (obj.hideFlags & HideFlags.HideInHierarchy) == 0) continue;
                if (refs.IndexOf(obj) != -1 || AssetDatabase.GetAssetPath(obj) != refsPath) continue;
                refs.Add(obj);
                GetHiddenReferences(obj, refsPath, refs);
            }

            return refs;
        }

        private static string GetFileExtension(Object obj)
        {
            return obj switch
            {
                AnimationClip _ => ".anim",
                AnimatorController _ => ".controller",
                AnimatorOverrideController _ => ".overrideController",
                Material _ => ".mat",
                Cubemap _ => ".cubemap",
                Texture _ => ".png",
                ComputeShader _ => ".compute",
                Shader _ => ".shader",
                Flare _ => ".flare",
                ShaderVariantCollection _ => ".shadervariants",
                LightmapParameters _ => ".giparams",
                GUISkin _ => ".guiskin",
#if UNITY_6000_0_OR_NEWER
                PhysicsMaterial _ => ".physicMaterial",
#else
                PhysicMaterial _ => ".physicMaterial",
#endif
                PhysicsMaterial2D _ => ".physicsMaterial2D",
                AudioMixer _ => ".mixer",
                SpriteAtlas _ => ".spriteatlas",
                TextAsset _ => ".txt",
                GameObject _ => ".prefab",
                ScriptableObject _ => ".asset",
                _ => null
            };
        }

    }
}
