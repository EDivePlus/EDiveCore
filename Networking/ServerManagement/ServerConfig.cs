// Author: František Holubec
// Created: 14.07.2025

using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerConfig : ScriptableObject
    {
        [JsonProperty("ServerName")]
        [SerializeField]
        private string _ServerName;
        
        [JsonProperty("MaxPlayers")]
        [SerializeField]
        private int _MaxPlayers;

        public long ServerID { get; set; }

        public string ServerName
        {
            get => _ServerName; 
            set => _ServerName = value;
        }

        public int MaxPlayers
        {
            get => _MaxPlayers; 
            set => _MaxPlayers = value;
        }
    }
}
