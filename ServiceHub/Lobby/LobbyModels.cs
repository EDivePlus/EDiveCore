// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.Lobby
{
    // --------
    // Requests
    // --------

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class RegisterServerRequest
    {
        [JsonProperty("app_secret")]
        [SerializeField]
        private string _AppSecret;

        [JsonProperty("name")]
        [SerializeField]
        private string _Name;

        [JsonProperty("version")]
        [SerializeField]
        private string _Version;

        [JsonProperty("public_address")]
        [SerializeField]
        private string _PublicAddress;

        [JsonProperty("public_port")]
        [SerializeField]
        private int? _PublicPort;

        [JsonProperty("relay_code")]
        [SerializeField]
        private string _RelayCode;

        [JsonProperty("data")]
        [SerializeField]
        private string _Data;

        [JsonProperty("debug")]
        [SerializeField]
        private bool _IsDebug;

        [JsonProperty("private")]
        [SerializeField]
        private bool _IsPrivate;

        [JsonProperty("join_code")]
        [SerializeField]
        private string _JoinCode;
        
        [JsonProperty("instance_id")]
        [SerializeField]
        private string _InstanceId;

        public RegisterServerRequest(
            string name,
            string version,
            string publicAddress = null,
            int? publicPort = null,
            string relayCode = null,
            string data = null,
            bool isPrivate = false,
            string joinCode = null,
            string instanceId = null,
            bool isDebug = false)
        {
            _Name = name;
            _Version = version;
            _PublicAddress = publicAddress;
            _PublicPort = publicPort;
            _RelayCode = relayCode;
            _Data = data;
            _IsPrivate = isPrivate;
            _JoinCode = joinCode;
            _InstanceId = instanceId;
            _IsDebug = isDebug;
        }

        public string AppSecret { get => _AppSecret; set => _AppSecret = value; }
        public string Name => _Name;
        public string Version => _Version;
        public string PublicAddress => _PublicAddress;
        public int? PublicPort => _PublicPort;
        public string RelayCode => _RelayCode;
        public string Data => _Data;
        public bool IsDebug => _IsDebug;
        public bool IsPrivate => _IsPrivate;
        public string InstanceId => _InstanceId;
        public string JoinCode => _JoinCode;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class UpdateServerRequest
    {
        [JsonProperty("secret")]
        [SerializeField]
        private string _Secret;

        [JsonProperty("name")]
        [SerializeField]
        private string _Name;

        [JsonProperty("data")]
        [SerializeField]
        private string _Data;

        [JsonProperty("current_players")]
        [SerializeField]
        private int? _CurrentPlayers;

        [JsonProperty("max_players")]
        [SerializeField]
        private int? _MaxPlayers;

        public UpdateServerRequest(string secret, string name = null, string data = null, int? currentPlayers = null, int? maxPlayers = null)
        {
            _Secret = secret;
            _Name = name;
            _Data = data;
            _CurrentPlayers = currentPlayers;
            _MaxPlayers = maxPlayers;
        }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class HeartbeatServerRequest
    {
        [JsonProperty("secret")]
        [SerializeField]
        private string _Secret;

        public HeartbeatServerRequest(string secret) { _Secret = secret; }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class DisposeServerRequest
    {
        [JsonProperty("secret")]
        [SerializeField]
        private string _Secret;

        public DisposeServerRequest(string secret) { _Secret = secret; }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class GetServerRequest
    {
        [JsonProperty("app_secret")]
        [SerializeField]
        private string _AppSecret;

        [JsonProperty("join_code")]
        [SerializeField]
        private string _JoinCode;

        public GetServerRequest(string joinCode, string appSecret = null)
        {
            _JoinCode = joinCode;
            _AppSecret = appSecret;
        }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class QueryServersRequest
    {
        [JsonProperty("app_secret")]
        [SerializeField]
        private string _AppSecret;

        [JsonProperty("count")]
        [SerializeField]
        private int? _Count;

        [JsonProperty("skip")]
        [SerializeField]
        private int? _Skip;

        public QueryServersRequest(int? count = null, int? skip = null, string appSecret = null)
        {
            _Count = count;
            _Skip = skip;
            _AppSecret = appSecret;
        }
    }

    // ---------
    // Responses
    // ---------

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class LobbyServerResponse
    {
        [JsonProperty("name")]
        [SerializeField]
        private string _Name;

        [JsonProperty("version")]
        [SerializeField]
        private string _Version;
        
        [JsonProperty("instance_id")]
        [SerializeField]
        private string _InstanceId;

        [JsonProperty("public_address")]
        [SerializeField]
        private string _PublicAddress;

        [JsonProperty("public_port")]
        [SerializeField]
        private int? _PublicPort;

        [JsonProperty("relay_code")]
        [SerializeField]
        private string _RelayCode;

        [JsonProperty("data")]
        [SerializeField]
        private string _Data;

        [JsonProperty("debug")]
        [SerializeField]
        private bool _IsDebug;

        [JsonProperty("private")]
        [SerializeField]
        private bool _IsPrivate;

        [JsonProperty("join_code")]
        [SerializeField]
        private string _JoinCode;

        public string Name => _Name;
        public string Version => _Version;
        public string InstanceId => _InstanceId;
        public string PublicAddress => _PublicAddress;
        public int? PublicPort => _PublicPort;
        public string RelayCode => _RelayCode;
        public string Data => _Data;
        public bool IsDebug => _IsDebug;
        public bool IsPrivate => _IsPrivate;
        public string JoinCode => _JoinCode;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerRegistrationResponse
    {
        [JsonProperty("secret")]
        [SerializeField]
        private string _Secret;

        [JsonProperty("code")]
        [SerializeField]
        private string _Code;

        public string Secret => _Secret;
        public string Code => _Code;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class LobbyEmptyResponse { }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class LobbyServerListResponse
    {
        public List<LobbyServerResponse> Items;
    }
}
