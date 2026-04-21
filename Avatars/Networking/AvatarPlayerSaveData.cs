// Author: Michal Petr
// Created: 21.04.2026

using System;
using EDIVE.UserCenter.SaveData;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    [JsonObject(MemberSerialization.OptIn)]
    [Serializable]
    public class AvatarPlayerSaveData : ASaveDataObject
    {
        public const string KEY = "PlayerAvatar";

        [SerializeField, JsonProperty("avatar_def")]
        private AvatarDefinition _PlayerAvatar;
        public AvatarDefinition PlayerAvatar
        {
            get => _PlayerAvatar;
            set => SetProperty(ref _PlayerAvatar, value);
        }
    }
}
