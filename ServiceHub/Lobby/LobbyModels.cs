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
    
    [JsonObject(MemberSerialization.OptIn)]
    public class RegisterServerRequest
    {
        [JsonProperty("app_secret")]
        public string AppSecret;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("version")]
        public string Version;

        [JsonProperty("public_address")]
        public string PublicAddress;

        [JsonProperty("public_port")]
        public int? PublicPort;

        [JsonProperty("max_players")]
        public int? MaxPlayers;

        [JsonProperty("data")]
        public string Data;

        [JsonProperty("debug")]
        public bool IsDebug;

        [JsonProperty("private")]
        public bool IsPrivate;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UpdateServerRequest
    {
        [JsonProperty("secret")]
        public string Secret;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("data")]
        public string Data;

        [JsonProperty("current_players")]
        public int? CurrentPlayers;
    }
    
    [JsonObject(MemberSerialization.OptIn)]
    public class HeartbeatServerRequest
    {
        [JsonProperty("secret")]
        public string Secret;

        public HeartbeatServerRequest(string secret)
        {
            Secret = secret;
        }
    }
    
    [JsonObject(MemberSerialization.OptIn)]
    public class DisposeServerRequest
    {
        [JsonProperty("secret")]
        public string Secret;

        public DisposeServerRequest(string secret)
        {
            Secret = secret;
        }
    }
    
    [JsonObject(MemberSerialization.OptIn)]
    public class GetServerRequest
    {
        [JsonProperty("app_secret")]
        public string AppSecret;

        [JsonProperty("join_code")]
        public string JoinCode;

        public GetServerRequest(string appSecret, string joinCode)
        {
            AppSecret = appSecret;
            JoinCode = joinCode;
        }
    }
    
    [JsonObject(MemberSerialization.OptIn)]
    public class QueryServersRequest
    {
        [JsonProperty("app_secret")]
        public string AppSecret;

        [JsonProperty("count")]
        public int? Count;

        [JsonProperty("skip")]
        public int? Skip;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("version")]
        public string Version;

        [JsonProperty("min_version")]
        public string MinVersion;

        [JsonProperty("max_version")]
        public string MaxVersion;

        [JsonProperty("version_segments")]
        public int? VersionSegments;

        [JsonProperty("debug")]
        public bool? IsDebug;

        [JsonProperty("private")]
        public bool? IsPrivate;

        [JsonProperty("not_full")]
        public bool? NotFull;

        [JsonProperty("has_players")]
        public bool? HasPlayers;

        [JsonProperty("min_players")]
        public int? MinPlayers;

        [JsonProperty("max_players_filter")]
        public int? MaxPlayersFilter;

        [JsonProperty("data")]
        public string Data;
    }

    // ---------
    // Responses
    // ---------
    
    [JsonObject(MemberSerialization.OptIn)]
    public class LobbyServerResponse
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("version")]
        public string Version;

        [JsonProperty("public_address")]
        public string PublicAddress;

        [JsonProperty("public_port")]
        public int? PublicPort;

        [JsonProperty("data")]
        public string Data;

        [JsonProperty("debug")]
        public bool IsDebug;

        [JsonProperty("private")]
        public bool IsPrivate;

        [JsonProperty("join_code")]
        public string JoinCode;

        [JsonProperty("current_players")]
        public int CurrentPlayers;

        [JsonProperty("max_players")]
        public int MaxPlayers;
    }
    
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerRegistrationResponse
    {
        [JsonProperty("secret")]
        public string Secret;

        [JsonProperty("code")]
        public string Code;
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
