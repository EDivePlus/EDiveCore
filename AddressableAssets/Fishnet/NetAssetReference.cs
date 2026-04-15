// Author: František Holubec
// Created: 15.04.2026

#if FISHNET
using FishNet.CodeGenerating;
using FishNet.Serializing;
using UnityEngine.AddressableAssets;

namespace EDIVE.AddressableAssets.Fishnet
{
    [UseGlobalCustomSerializer]
    public class NetAssetReference
    {
        public AssetReference Reference { get; }
        
        public NetAssetReference() { }
        public NetAssetReference(AssetReference reference) => Reference = reference;
        
        public static implicit operator NetAssetReference(AssetReference r) => new(r);
        public static implicit operator AssetReference(NetAssetReference w) => w.Reference;
    }
    
    public static class FormDefinitionNetworkExtensions
    {
        public static void WriteNetAssetReference(this Writer writer, NetAssetReference value) => writer.WriteString(value?.Reference != null ? value.Reference.AssetGUID : string.Empty);
        public static NetAssetReference ReadNetAssetReference(this Reader reader) => new AssetReference(reader.ReadStringAllocated());
    }
}
#endif