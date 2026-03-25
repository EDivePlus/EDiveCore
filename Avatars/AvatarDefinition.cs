using EDIVE.AssetTranslation;
using EDIVE.VisualPresets.Presets;
using FishNet.CodeGenerating;
using FishNet.Serializing;
using JetBrains.Annotations;
using UnityEngine;

namespace EDIVE.Avatars
{
    [UseGlobalCustomSerializer]
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

    // Used by Fishet for serialization of AvatarDefinition references.
    [UsedImplicitly] 
    public static class AvatarDefinitionExtensions
    {
        public static void WriteAvatarDefinition(this Writer writer, AvatarDefinition value) => writer.CustomWriteTranslatedDefinition(value);
        public static AvatarDefinition ReadAvatarDefinition(this Reader reader) => reader.CustomReadTranslatedDefinition<AvatarDefinition>();
    }
}
