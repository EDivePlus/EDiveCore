// Author: František Holubec
// Created: 21.07.2025

#if MEMORY_PACK
using MemoryPack;
using MemoryPack.Internal;
using UnityEngine;

namespace EDIVE.AssetTranslation
{
    [Preserve]
    public class AssetTranslationMemoryPackFormatter<TDefinition> : MemoryPackFormatter<TDefinition> where TDefinition : ScriptableObject, IUniqueDefinition
    {
        [Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref TDefinition value)
        {
            if (value == null)
            {
                writer.WriteString(null);
                return;
            }
            
            writer.WriteString(value.UniqueID);
        }

        [Preserve]
        public override void Deserialize(ref MemoryPackReader reader, ref TDefinition value)
        {
            var id = reader.ReadString();
            if (id == null)
            {
                value = null;
                return;
            }

            DefinitionTranslationUtils.TryGetDefinition(id, out value);
        }
    }
}
#endif
