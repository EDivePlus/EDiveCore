// Author: Michal Petr
// Created: 17.03.2026

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EDIVE.UserCenter.SaveData
{
    [Serializable]
    public class SaveDataResponse
    {
        [JsonProperty("key")]
        private string Key;

        [JsonProperty("value")]
        public JToken Value;

        [JsonProperty("created_at")]
        public DateTime CreatedAt;

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt;
    }
    
    [Serializable]
    public class SaveDataKeyListResponse
    {
        [JsonProperty("keys")]
        [SerializeField]
        private List<string> _Keys;
        public IReadOnlyList<string> Keys => _Keys;
    }
    
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class SaveDataWriteRequest
    {
        [JsonProperty("value")]
        private JRaw _Value;

        public SaveDataWriteRequest(string serializedJson)
        {
            _Value = new JRaw(serializedJson);
        }
    }
}
