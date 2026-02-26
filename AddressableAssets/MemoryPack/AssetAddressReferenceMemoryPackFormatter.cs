// Author: František Holubec
// Created: 26.02.2026

#if MEMORY_PACK
using MemoryPack;
using MemoryPack.Internal;
using UnityEngine;

namespace EDIVE.AddressableAssets.MemoryPack
{
    [Preserve]
    public class AssetAddressReferenceMemoryPackFormatter<T> : MemoryPackFormatter<AssetAddressReference<T>> where T : Object
    {
        [Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref AssetAddressReference<T> value)
        {
            if (value == null)
            {
                writer.WriteString(null);
                return;
            }
            
            writer.WriteString(value.Address);
        }

        [Preserve]
        public override void Deserialize(ref MemoryPackReader reader, ref AssetAddressReference<T> value)
        {
            var address = reader.ReadString();
            if (address == null)
            {
                value = null;
                return;
            }
            
            value = new AssetAddressReference<T>(address);
        }
    }
}
#endif
