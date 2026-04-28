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

        [JsonProperty("Port")]
        [SerializeField]
        [Tooltip("Game port for the host transport. Set to 0 to assign a free port dynamically (useful for hosting multiple servers on one machine).")]
        private ushort _Port;

        [JsonProperty("ServerID")]
        private long _serverID;

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

        public ushort Port
        {
            get => _Port;
            set => _Port = value;
        }

        public long ServerID
        {
            get => _serverID;
            set => _serverID = value;
        }
    }
}
