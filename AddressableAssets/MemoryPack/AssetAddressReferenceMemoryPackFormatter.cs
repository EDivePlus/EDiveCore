// Author: František Holubec
// Created: 26.02.2026

#if MEMORY_PACK
using System;
using MemoryPack;
using MemoryPack.Internal;

namespace EDIVE.AddressableAssets.MemoryPack
{
    [Preserve]
    public class AssetAddressReferenceMemoryPackFormatter<T> : MemoryPackFormatter<T> where T : AssetAddressReference
    {
        [Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref T value)
        {
            if (value == null)
            {
                writer.WriteString(null);
                return;
            }
            
            writer.WriteString(value.Address);
        }

        [Preserve]
        public override void Deserialize(ref MemoryPackReader reader, ref T value)
        {
            var address = reader.ReadString();
            if (address == null)
            {
                value = null;
                return;
            }
            
            value = Activator.CreateInstance(typeof(T), address) as T;
        }
    }
}
#endif
