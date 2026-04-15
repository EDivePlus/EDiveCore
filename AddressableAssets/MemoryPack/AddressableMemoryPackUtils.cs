// Author: František Holubec
// Created: 26.02.2026

#if MEMORY_PACK
using MemoryPack;
using MemoryPack.Formatters;

namespace EDIVE.AddressableAssets.MemoryPack
{
    public static class AddressableMemoryPackUtils
    {
        public static void RegisterAssetAddressReferenceFormatter<T>() where T : AssetAddressReference
        {
            MemoryPackFormatterProvider.Register(new AssetAddressReferenceMemoryPackFormatter<T>());
            MemoryPackFormatterProvider.Register(new ArrayFormatter<T>());
            MemoryPackFormatterProvider.Register(new ListFormatter<T>());
        }
    } 
}
#endif
