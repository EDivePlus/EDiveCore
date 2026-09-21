// Author: Michal Petr
// Created: 21.09.2026

#if PURRNET
using EDIVE.AssetTranslation;
using JetBrains.Annotations;
using PurrNet.Packing;

namespace EDIVE.VisualPresets.UVDecals
{
    [UsedImplicitly]
    public static class VisualPresetUVDecalsBitPackerExtensions
    {
        public static void Write(this BitPacker packer, UVDecalVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            packer.CustomWriteTranslatedDefinition(value?.Decal);
        }

        public static void Read(this BitPacker packer, ref UVDecalVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<UVDecalVisualID>();
            var decal = packer.CustomReadTranslatedDefinition<UVDecalDefinition>();
            value = new UVDecalVisualPresetRecord(id, decal);
        }
    }
}
#endif
