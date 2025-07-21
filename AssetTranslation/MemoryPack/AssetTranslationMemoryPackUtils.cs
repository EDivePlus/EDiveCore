// Author: František Holubec
// Created: 21.07.2025

#if MEMORY_PACK
using MemoryPack;
using MemoryPack.Formatters;
using UnityEngine;

namespace EDIVE.AssetTranslation
{
    public static class AssetTranslationMemoryPackUtils
    {
        public static void RegisterTranslator<TDefinition>() where TDefinition : ScriptableObject, IUniqueDefinition
        {
            MemoryPackFormatterProvider.Register(new AssetTranslationMemoryPackFormatter<TDefinition>());
            MemoryPackFormatterProvider.Register(new ArrayFormatter<TDefinition>());
            MemoryPackFormatterProvider.Register(new ListFormatter<TDefinition>());
        }
    }
}
#endif
