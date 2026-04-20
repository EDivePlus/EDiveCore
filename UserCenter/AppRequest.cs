// Author: Michal Petr
// Created: 08.04.2026

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class AppRequest
    {
        [JsonProperty("app_secret"), SerializeField]
        protected string _AppSecret;
        
        public AppRequest(string appSecret)
        {
            _AppSecret = appSecret;
        }
    }
    
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class AppRequest<T> : AppRequest
    {
        [JsonProperty("data"), SerializeField]
        private T _Data;
        
        public AppRequest(string appSecret, T data) : base(appSecret)
        {
            _Data = data;
        }
    }
}
