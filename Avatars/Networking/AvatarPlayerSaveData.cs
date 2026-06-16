// Author: Michal Petr
// Created: 21.04.2026

using System;
using EDIVE.ServiceHub.SaveData;
using EDIVE.VisualPresets.Presets;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    [JsonObject(MemberSerialization.OptIn)]
    [Serializable]
    public class AvatarPlayerSaveData : ASaveDataObject
    {
        public const string KEY = "PlayerAvatar";
        public override string Key => KEY;

        public event Action<VisualPreset> CustomizationChanged;

        [SerializeField, JsonProperty("avatar_def")]
        private AvatarDefinition _PlayerAvatar;
        public AvatarDefinition PlayerAvatar
        {
            get => _PlayerAvatar;
            set => SetProperty(ref _PlayerAvatar, value);
        }
        
        [SerializeField, JsonProperty("visual_preset")]
        private VisualPreset _CustomizationPreset;
        public VisualPreset CustomizationPreset
        {
            get => _CustomizationPreset;
            set
            {
                SetProperty(ref _CustomizationPreset, value);
                CustomizationChanged?.Invoke(value);
            }
        }
    }
}
