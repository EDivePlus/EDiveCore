using EDIVE.AssetTranslation;
using EDIVE.VisualPresets.Presets;
using JetBrains.Annotations;
using PurrNet.Packing;
using UnityEngine;

namespace EDIVE.Avatars
{
    public class AvatarDefinition : AUniqueDefinition
    {
        [SerializeField]
        private AvatarController _AvatarPrefab;

        [SerializeField]
        private VisualPreset _Visual;

        public AvatarController AvatarPrefab => _AvatarPrefab;
        public VisualPreset Visual => _Visual;

        public bool IsValid() => _AvatarPrefab != null;
    }

    // Used by PurrNet for serialization of AvatarDefinition references.
    // PurrNet auto-discovers static classes that contain `Write(this BitPacker, T)` and
    // `Read(this BitPacker, ref T)` extension method pairs.
    [UsedImplicitly]
    public static class AvatarDefinitionExtensions
    {
        public static void Write(this BitPacker packer, AvatarDefinition value) => packer.CustomWriteTranslatedDefinition(value);
        public static void Read(this BitPacker packer, ref AvatarDefinition value) => value = packer.CustomReadTranslatedDefinition<AvatarDefinition>();
    }
}
