using UnityEngine.AddressableAssets;

namespace EDIVE.AddressableAssets
{
    public static class AssetReferenceExtensions
    {
        public static string GetAssetName(this AssetReference assetReference)
        {
            if (assetReference.Asset != null)
                return assetReference.Asset.name;
#if UNITY_EDITOR
            if (assetReference.editorAsset != null)
                return assetReference.editorAsset.name;
#endif
            return $"Not Loaded (GUID: '{assetReference.AssetGUID}')";
        }
    }
}
