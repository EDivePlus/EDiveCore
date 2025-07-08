// Author: František Holubec
// Created: 07.07.2025

#if MEMORY_PACK
using System;

namespace EDIVE.Utils.MemoryPack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class MemoryPackUnionTagAttribute : Attribute
    {
        public ushort Tag { get; }

        public MemoryPackUnionTagAttribute(ushort tag)
        {
            Tag = tag;
        }
    }
}
#endif
