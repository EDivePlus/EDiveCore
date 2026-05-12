// Author: Michal Petr
// Created: 12.05.2026

using System;
using EDIVE.Time.DateTimeUtils;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentItemInfo
    {
        [JsonProperty("id")]
        [SerializeField]
        private string _Id;

        [JsonProperty("name")]
        [SerializeField]
        private string _Name;

        [JsonProperty("original_file_name")]
        [SerializeField]
        private string _OriginalFileName;

        [JsonProperty("extension")]
        [SerializeField]
        private string _Extension;

        [JsonProperty("content_type")]
        [SerializeField]
        private string _ContentType;

        [JsonProperty("size_bytes")]
        [SerializeField]
        private long _SizeBytes;

        [JsonProperty("media_type_key")]
        [SerializeField]
        private string _MediaTypeKey;

        [JsonProperty("media_type_display_name")]
        [SerializeField]
        private string _MediaTypeDisplayName;

        [JsonProperty("created_at")]
        [SerializeField]
        private UDateTime _CreatedAt;

        [JsonProperty("updated_at")]
        [SerializeField]
        private UDateTime _UpdatedAt;
        
        [JsonProperty("owner")]
        [SerializeField]
        private ContentItemOwnerInfo _Owner;

        public string Id => _Id;
        public string Name => _Name;
        public string OriginalFileName => _OriginalFileName;
        public string Extension => _Extension;
        public string ContentType => _ContentType;
        public long SizeBytes => _SizeBytes;
        public string MediaTypeKey => _MediaTypeKey;
        public string MediaTypeDisplayName => _MediaTypeDisplayName;
        public DateTime CreatedAt => _CreatedAt;
        public DateTime UpdatedAt => _UpdatedAt;
        public ContentItemOwnerInfo Owner => _Owner;
    }
    
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentItemOwnerInfo
    {
        [JsonProperty("id")]
        [SerializeField]
        private string _Id;

        [JsonProperty("display_name")]
        [SerializeField]
        private string _DisplayName;

        [JsonProperty("avatar_url")]
        [SerializeField]
        private string _AvatarUrl;
        
        public string Id => _Id;
        public string DisplayName => _DisplayName;
        public string AvatarUrl => _AvatarUrl;
    }
}
