// Author: Michal Petr
// Created: 17.03.2026

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData
{
    [Serializable]
    public class SaveDataResponse
    {
        [JsonProperty("key")]
        public string Key;

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

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class SaveDataBatchWriteRequest
    {
        [JsonProperty("entries")]
        private Dictionary<string, JRaw> _Entries;

        public SaveDataBatchWriteRequest(IReadOnlyDictionary<string, string> entries)
        {
            _Entries = new Dictionary<string, JRaw>(entries.Count);
            foreach (var (key, json) in entries)
                _Entries[key] = new JRaw(json);
        }
    }

    [Serializable]
    public class SaveDataBatchResponse
    {
        [JsonProperty("saved")]
        public List<SaveDataResponse> Saved;

        [JsonProperty("errors")]
        public Dictionary<string, string> Errors;
    }
}
