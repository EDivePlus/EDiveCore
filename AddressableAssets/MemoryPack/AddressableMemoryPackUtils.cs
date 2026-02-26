// Author: František Holubec
// Created: 26.02.2026

using MemoryPack;
using MemoryPack.Formatters;

namespace EDIVE.AddressableAssets.MemoryPack
{
    public static class AddressableMemoryPackUtils
    {
        public static void RegisterAssetAddressFormatter<T>() where T : UnityEngine.Object
        {
            MemoryPackFormatterProvider.Register(new AssetAddressReferenceMemoryPackFormatter<T>());
            MemoryPackFormatterProvider.Register(new ArrayFormatter<AssetAddressReference<T>>());
            MemoryPackFormatterProvider.Register(new ListFormatter<AssetAddressReference<T>>());
        }
    } 
}
