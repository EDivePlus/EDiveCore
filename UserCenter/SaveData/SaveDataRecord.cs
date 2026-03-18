// Author: Michal Petr
// Created: 17.03.2026

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter.SaveData
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
        private string _JsonData;
        public string JsonData => _JsonData;
        
        [SerializeField, JsonProperty("userUuid")]
        private string _UserUuid;
        public string UserUuid => _UserUuid;
        
        public SaveDataRecord(long id, string key, string jsonData, string userUuid)
        {
            _ID = id;
            _Key = key;
            _JsonData = jsonData;
            _UserUuid = userUuid;
        }
    }
}
