// Author: Radim Holub
// Created: 19.02.2026

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter
{
    // Common models
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class PlayerProfileJson
    {
        [SerializeField, JsonProperty("username")]
        private string _Username;
        public string Username => _Username;
        
        [SerializeField, JsonProperty("avatarId")]
        private string _AvatarId;
        public string AvatarId => _AvatarId;
        
        public PlayerProfileJson(string username, string avatarId)
        {
            _Username = username;
            _AvatarId = avatarId;
        }
    }
    
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class SaveDataRecord
    {
        [SerializeField, JsonProperty("id")]
        private long _ID;
        public long ID => _ID;
        
        [SerializeField, JsonProperty("key")]
        private string _Key;
        public string Key => _Key;
        
        [SerializeField, JsonProperty("description")]
        private string _Description;
        public string Description => _Description;
        
        [SerializeField, JsonProperty("userUuid")]
        private string _UserUuid;
        public string UserUuid => _UserUuid;
        
        [SerializeField, JsonProperty("userBasicPojo")]
        private UserBasicPojo _UserBasicPojo;
        public UserBasicPojo UserBasicPojo => _UserBasicPojo;
        
        public SaveDataRecord(long id, string key, string description, string userUuid, UserBasicPojo userBasicPojo)
        {
            _ID = id;
            _Key = key;
            _Description = description;
            _UserUuid = userUuid;
            _UserBasicPojo = userBasicPojo;
        }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class UserBasicPojo
    {
        [SerializeField, JsonProperty("uuid")]
        private string _Uuid;
        public string Uuid => _Uuid;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentWrapper<TItem>
    {
        [SerializeField, JsonProperty("content")]
        private List<TItem> _Content;
        public List<TItem> Content => _Content;
    }
    
    // Response wrappers
    public enum DataStatus
    {
        Ok,
        NotFound,
        Error
    }

    public readonly struct DataResult<T>
    {
        public DataStatus Status { get; }
        public T Value { get; }
        public string ErrorMessage { get; }
        public bool FromServer { get; }
        public bool FromLocal { get; }

        public bool IsOk => Status == DataStatus.Ok;
        public bool IsNotFound => Status == DataStatus.NotFound;

        private DataResult(DataStatus status, T value, string errorMessage, bool fromServer, bool fromLocal, bool fromMemory)
        {
            Status = status;
            Value = value;
            ErrorMessage = errorMessage;
            FromServer = fromServer;
            FromLocal = fromLocal;
        }

        public static DataResult<T> Ok(T value, bool fromServer, bool fromLocal, bool fromMemory = false)
            => new(DataStatus.Ok, value, null, fromServer, fromLocal, fromMemory);

        public static DataResult<T> NotFound()
            => new(DataStatus.NotFound, default, null, false, false, false);

        public static DataResult<T> Error(string error)
            => new(DataStatus.Error, default, error, false, false, false);
    }

    public readonly struct NetworkResponse<T>
    {
        public bool Success { get; }
        public long StatusCode { get; }
        public string Error { get; }
        public string Raw { get; }
        public T Result { get; }

        public bool IsNotFound => StatusCode == 404;

        private NetworkResponse(bool success, long statusCode, string error, string raw, T result)
        {
            Success = success;
            StatusCode = statusCode;
            Error = error;
            Raw = raw;
            Result = result;
        }

        public static NetworkResponse<T> Ok(long status, T result, string raw)
            => new NetworkResponse<T>(true, status, null, raw, result);

        public static NetworkResponse<T> Fail(long status, string error, string raw)
            => new NetworkResponse<T>(false, status, error, raw, default);
    }
}


