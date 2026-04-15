// Author: Radim Holub
// Created: 19.02.2026

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ApiResponse<T>
    {
        [SerializeField, JsonProperty("status")]
        private int _Status;

        [SerializeField, JsonProperty("message")]
        private string _Message;

        [SerializeField, JsonProperty("data")]
        private T _Data;

        public int Status => _Status;
        public string Message => _Message;
        public T Data => _Data;
    }
}
