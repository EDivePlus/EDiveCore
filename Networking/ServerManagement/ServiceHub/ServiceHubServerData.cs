// Author: Michal Petr
// Created: 12.05.2026

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.ServiceHub
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ServiceHubServerData
    {
        [JsonProperty("instance_id")]
        [SerializeField]
        private string _InstanceID;

        [JsonProperty("current_players")]
        [SerializeField]
        private int _CurrentPlayers;

        [JsonProperty("max_players")]
        [SerializeField]
        private int _MaxPlayers;

        public ServiceHubServerData() { }

        public ServiceHubServerData(string instanceID, int currentPlayers, int maxPlayers)
        {
            _InstanceID = instanceID;
            _CurrentPlayers = currentPlayers;
            _MaxPlayers = maxPlayers;
        }

        public string InstanceID => _InstanceID;
        public int CurrentPlayers => _CurrentPlayers;
        public int MaxPlayers => _MaxPlayers;

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
