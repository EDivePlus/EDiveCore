// Author: Michal Petr
// Created: 13.05.2026

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.Probe
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ProbeRequest
    {
        [JsonProperty("port")]
        [SerializeField]
        private int _Port;

        [JsonProperty("token")]
        [SerializeField]
        private string _Token;

        [JsonProperty("timeout_ms")]
        [SerializeField]
        private int _TimeoutMs;

        public ProbeRequest(int port, string token, int timeoutMs)
        {
            _Port = port;
            _Token = token;
            _TimeoutMs = timeoutMs;
        }

        public int Port => _Port;
        public string Token => _Token;
        public int TimeoutMs => _TimeoutMs;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ProbeResponse
    {
        [JsonProperty("reachable")]
        [SerializeField]
        private bool _Reachable;

        [JsonProperty("public_address")]
        [SerializeField]
        private string _PublicAddress;

        public bool Reachable => _Reachable;
        public string PublicAddress => _PublicAddress ?? string.Empty;
    }
}
