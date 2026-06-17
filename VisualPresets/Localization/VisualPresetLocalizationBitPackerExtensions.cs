// Author: František Holubec
// Created: 17.06.2026

#if PURRNET && UNITY_LOCALIZATION
using System.Text;
using EDIVE.AssetTranslation;
using EDIVE.Localization;
using EDIVE.VisualPresets.VisualIDs;
using JetBrains.Annotations;
using PurrNet.Packing;

namespace EDIVE.VisualPresets.Localization
{
    [UsedImplicitly]
    public static class VisualPresetLocalizationBitPackerExtensions
    {
        public static void Write(this BitPacker packer, LocalizedStringVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            packer.WriteString(Encoding.UTF8, value?.LocalizedText?.Term ?? string.Empty);
        }

        public static void Read(this BitPacker packer, ref LocalizedStringVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<StringVisualID>();
            var term = packer.ReadString(Encoding.UTF8);
            value = new LocalizedStringVisualPresetRecord(id, new SafeLocalizedString(term));
        }
    }
}
#endif
