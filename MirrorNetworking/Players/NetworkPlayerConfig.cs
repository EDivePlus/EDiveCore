// Author: František Holubec
// Created: 03.04.2025

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Edive.Networking
{
    [JsonObject(MemberSerialization.OptIn)]
    public class NetworkPlayerConfig : ScriptableObject
    {
        [SerializeField]
        [JsonProperty("Roles")]
        private List<NetworkRoleRecord> _Roles = new();

        public IReadOnlyList<NetworkRoleRecord> Roles => _Roles;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class NetworkRoleRecord
    {
        [SerializeField]
        [JsonProperty("Role")]
        private string _Role;

        [SerializeField]
        [JsonProperty("Password")]
        private string _Password;

        public string Role => _Role;
        public string Password => _Password;
    }
}
