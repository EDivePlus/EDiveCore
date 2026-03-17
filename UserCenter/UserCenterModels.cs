// Author: Radim Holub
// Created: 19.02.2026

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter
{
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
        public bool FromRemote { get; }

        public bool IsOk => Status == DataStatus.Ok;
        public bool IsNotFound => Status == DataStatus.NotFound;
        public bool FromLocal => !FromRemote;

        private DataResult(DataStatus status, T value, string errorMessage, bool fromRemote)
        {
            Status = status;
            Value = value;
            ErrorMessage = errorMessage;
            FromRemote = fromRemote;
        }

        public static DataResult<T> Ok(T value, bool fromServer)
            => new(DataStatus.Ok, value, null, fromServer);

        public static DataResult<T> NotFound()
            => new(DataStatus.NotFound, default, null, false);

        public static DataResult<T> Error(string error, T value = default)
            => new(DataStatus.Error, value, error, false);
    }
}


