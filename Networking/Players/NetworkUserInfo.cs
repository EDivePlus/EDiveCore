// Author: Michal Petr
// Created: 21.05.2026

using System;
using EDIVE.ServiceHub.Auth;
using Newtonsoft.Json;
using PurrNet.Packing;
using UnityEngine;

namespace EDIVE.Networking.Players
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class NetworkUserInfo : IPackedAuto
    {
        [JsonProperty("id")]
        [SerializeField]
        private string _Id;

        [JsonProperty("email")]
        [SerializeField]
        private string _Email;

        [JsonProperty("name")]
        [SerializeField]
        private string _Name;

        public string Id => _Id;
        public string Email => _Email;
        public string Name => _Name;

        public NetworkUserInfo(string id, string email, string name)
        {
            _Id = id;
            _Email = email;
            _Name = name;
        }
        
        public static NetworkUserInfo FromAuthUserInfo(AuthUserInfo info)
        {
            return new NetworkUserInfo(info.Id, info.Email, info.Name);
        }
        
        public static NetworkUserInfo CreateAnonymous()
        {
            var id = Guid.NewGuid().ToString();
            return new NetworkUserInfo(id, "", $"Anonymous-{id[..4]}");
        }
    }
}
