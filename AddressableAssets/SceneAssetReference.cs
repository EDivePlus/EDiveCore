// Author: František Holubec
// Created: 08.04.2025

using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.AddressableAssets
{
    [System.Serializable]
    public class SceneAssetReference : AssetReference
    {
        public SceneAssetReference() { }
        public SceneAssetReference(string guid) : base(guid) { }

        /// <inheritdoc/>
        public override bool ValidateAsset(Object obj)
        {
#if UNITY_EDITOR
            var type = obj.GetType();
            return typeof(UnityEditor.SceneAsset).IsAssignableFrom(type);
#else
            return false;
#endif
        }

        /// <inheritdoc/>
        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            var type = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
            return typeof(UnityEditor.SceneAsset).IsAssignableFrom(type);
#else
            return path.EndsWith(".unity");
#endif
        }

#if UNITY_EDITOR
        // Type-specific override of parent editorAsset.  Used by the editor to represent the asset referenced.
        // ReSharper disable once InconsistentNaming
        public new UnityEditor.SceneAsset editorAsset => (UnityEditor.SceneAsset) base.editorAsset;
#endif
    }

}
