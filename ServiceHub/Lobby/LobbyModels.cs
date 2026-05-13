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

        [JsonProperty("current_players")]
        [SerializeField]
        private int? _CurrentPlayers;

        public UpdateServerRequest(string secret, string name = null, int? currentPlayers = null)
        {
            _Secret = secret;
            _Name = name;
            _CurrentPlayers = currentPlayers;
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

        [JsonProperty("name")]
        [SerializeField]
        private string _Name;

        [JsonProperty("version")]
        [SerializeField]
        private string _Version;

        [JsonProperty("debug")]
        [SerializeField]
        private bool? _IsDebug;

        [JsonProperty("private")]
        [SerializeField]
        private bool? _IsPrivate;

        [JsonProperty("not_full")]
        [SerializeField]
        private bool? _NotFull;

        [JsonProperty("has_players")]
        [SerializeField]
        private bool? _HasPlayers;

        [JsonProperty("min_players")]
        [SerializeField]
        private int? _MinPlayers;

        [JsonProperty("max_players_filter")]
        [SerializeField]
        private int? _MaxPlayersFilter;

        [JsonProperty("data")]
        [SerializeField]
        private string _Data;

        public string AppSecret { get => _AppSecret; set => _AppSecret = value; }
        public int? Count { get => _Count; set => _Count = value; }
        public int? Skip { get => _Skip; set => _Skip = value; }
        public string Name { get => _Name; set => _Name = value; }
        public string Version { get => _Version; set => _Version = value; }
        public bool? IsDebug { get => _IsDebug; set => _IsDebug = value; }
        public bool? IsPrivate { get => _IsPrivate; set => _IsPrivate = value; }
        public bool? NotFull { get => _NotFull; set => _NotFull = value; }
        public bool? HasPlayers { get => _HasPlayers; set => _HasPlayers = value; }
        public int? MinPlayers { get => _MinPlayers; set => _MinPlayers = value; }
        public int? MaxPlayersFilter { get => _MaxPlayersFilter; set => _MaxPlayersFilter = value; }
        public string Data { get => _Data; set => _Data = value; }
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

        [JsonProperty("current_players")]
        [SerializeField]
        private int _CurrentPlayers;

        [JsonProperty("max_players")]
        [SerializeField]
        private int _MaxPlayers;

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
        public int CurrentPlayers => _CurrentPlayers;
        public int MaxPlayers => _MaxPlayers;
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
