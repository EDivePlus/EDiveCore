// Author: František Holubec
// Created: 17.06.2026

#if PURRNET
using System.Text;
using EDIVE.AssetTranslation;
using JetBrains.Annotations;
using PurrNet.Packing;

namespace EDIVE.VisualPresets.StateHandling
{
    [UsedImplicitly]
    public static class VisualPresetStateHandlingBitPackerExtensions
    {
        public static void Write(this BitPacker packer, MultiStateVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            packer.WriteString(Encoding.UTF8, value?.State ?? string.Empty);
        }

        public static void Read(this BitPacker packer, ref MultiStateVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<MultiStateVisualID>();
            var state = packer.ReadString(Encoding.UTF8);
            value = new MultiStateVisualPresetRecord(id, state);
        }
    }
}
#endif
