// Author: František Holubec
// Created: 14.07.2025

using System;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
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
        [SerializeField]
        [Tooltip("Unique identifier for this server, generated in ServiceHub. If left empty, the server will not be registered with ServiceHub and no save data will be saved to it.")]
        private string _ServerID;
        
        [JsonProperty("ServerSecret")]
        [SerializeField]
        [Tooltip("Authentication key for this server, generated in ServiceHub. If left empty, the server will not be registered with ServiceHub and no save data will be saved to it.")]
        private string _ServerSecret;
        
        [ShowInInspector]
        [ReadOnly]
        public string InstanceID { get; set; }

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

        public string ServerID
        {
            get => _ServerID;
            set => _ServerID = value;
        }
        
        public string ServerSecret
        {
            get => _ServerSecret;
            set => _ServerSecret = value;
        }
    }
}
