// Author: Michal Petr
// Created: 17.03.2026

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter
{
    [JsonObject(MemberSerialization.OptIn)]
    [Serializable]
    public class UserCenterProfile
    {
        [SerializeField, JsonProperty("username")]
        private string _Username;
        public string Username => _Username;
    }
}
