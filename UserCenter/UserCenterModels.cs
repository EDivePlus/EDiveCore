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
}


