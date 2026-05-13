// Author: Michal Petr
// Created: 12.05.2026

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.ServiceHub
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ServiceHubServerData
    {
        [JsonProperty("instance_id")]
        public string InstanceID;
        
        [JsonProperty("relay_code")]
        public string RelayCode;

        public string Serialize() => JsonConvert.SerializeObject(this);

        public static ServiceHubServerData TryParse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;
            try { return JsonConvert.DeserializeObject<ServiceHubServerData>(raw); }
            catch { return null; }
        }
    }
}
