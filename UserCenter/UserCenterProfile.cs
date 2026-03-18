// Author: Michal Petr
// Created: 17.03.2026

using System;
using EDIVE.UserCenter.SaveData;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter
{
    [JsonObject(MemberSerialization.OptIn)]
    [Serializable]
    public class UserCenterProfile : ASaveDataObject
    {
        [SerializeField, JsonProperty("username")]
        private string _Username;
        public string Username
        {
            get => _Username;
            set => SetProperty(ref _Username, value);
        }
    }
}
