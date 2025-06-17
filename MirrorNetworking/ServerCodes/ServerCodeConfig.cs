// Author: František Holubec
// Created: 03.04.2025

using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.MirrorNetworking.ServerCodes
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerCodeConfig : ScriptableObject
    {
        [JsonProperty("ServerManagerUrl")]
        [SerializeField]
        private string _ServerManagerUrl = "";

        [JsonProperty("ServerTitle")]
        [SerializeField]
        private string _ServerTitle;

        [JsonProperty("AutoRegisterServer")]
        [SerializeField]
        private bool _AutoRegisterServer;

        [EnableIf(nameof(_AutoRegisterServer))]
        [JsonProperty("RegisterLocalIP")]
        [SerializeField]
        private bool _RegisterLocalIP;

        [JsonProperty("ServerCode")]
        [SerializeField]
        private string _ServerCode;

        [JsonProperty("Port")]
        [SerializeField]
        private int _Port;

        [JsonProperty("UpdateFrequency")]
        [SerializeField]
        private int _UpdateFrequency;

        public string ServerManagerUrl => _ServerManagerUrl;
        public string ServerTitle => _ServerTitle;
        public bool AutoRegisterServer => _AutoRegisterServer;
        public bool RegisterLocalIP => _RegisterLocalIP;
        public string ServerCode => _ServerCode;
        public int Port => _Port;
        public int UpdateFrequency => _UpdateFrequency;
    }
}
