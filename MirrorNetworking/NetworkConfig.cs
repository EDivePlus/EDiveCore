// Author: František Holubec
// Created: 22.03.2025

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace EDIVE.MirrorNetworking
{
    [GlobalConfig("Assets/_Project/Resources/")]
    [JsonObject(MemberSerialization.OptIn)]
    public class NetworkConfig : GlobalConfig<NetworkConfig>
    {
        [JsonProperty]
        [SerializeField]
        private string _ServerManagerUrl = "";

        [JsonProperty]
        [SerializeField]
        private string _ServerListingManagerSecret = "";

        [PropertySpace]
        [JsonProperty]
        [SerializeField]
        private string _ServerTitle;

        [JsonProperty]
        [SerializeField]
        private bool _AutoRegisterServer;

        [EnableIf(nameof(_AutoRegisterServer))]
        [JsonProperty]
        [SerializeField]
        private bool _RegisterLocalIP;

        [JsonProperty]
        [SerializeField]
        private string _ServerCode;

        [JsonProperty]
        [SerializeField]
        private int _Port;

        [JsonProperty]
        [SerializeField]
        private int _UpdateFrequency;

        [JsonProperty]
        [SerializeField]
        private List<NetworkRoleRecord> _Roles;

        public string ServerManagerUrl => _ServerManagerUrl;
        public string ServerListingManagerSecret => _ServerListingManagerSecret;

        public string ServerTitle => _ServerTitle;
        public bool AutoRegisterServer => _AutoRegisterServer;
        public bool RegisterLocalIP => _RegisterLocalIP;
        public string ServerCode => _ServerCode;
        public int Port => _Port;
        public int UpdateFrequency => _UpdateFrequency;
        public List<NetworkRoleRecord> Roles => _Roles;
    }

    [Serializable]
    public class NetworkRoleRecord
    {
        [SerializeField]
        private string _Role;

        [SerializeField]
        private string _Password;

        public string Role => _Role;
        public string Password => _Password;
    }
}
