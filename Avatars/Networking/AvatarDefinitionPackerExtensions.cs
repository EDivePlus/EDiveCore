// Author: František Holubec
// Created: 17.06.2026

using EDIVE.AssetTranslation;
using JetBrains.Annotations;
using PurrNet.Packing;

namespace EDIVE.Avatars.Networking
{
    [UsedImplicitly]
    public static class AvatarDefinitionPackerExtensions
    {
        public static void Write(this BitPacker packer, AvatarDefinition value) => packer.CustomWriteTranslatedDefinition(value);
        public static void Read(this BitPacker packer, ref AvatarDefinition value) => value = packer.CustomReadTranslatedDefinition<AvatarDefinition>();
    }
}
