// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Collections.Generic;
using EDIVE.Time.DateTimeUtils;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentItemSummary
    {
        [JsonProperty("id")]
        [SerializeField]
        private long _Id;

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

        public long Id => _Id;
        public string Name => _Name;
        public string OriginalFileName => _OriginalFileName;
        public string Extension => _Extension;
        public string ContentType => _ContentType;
        public long SizeBytes => _SizeBytes;
        public string MediaTypeKey => _MediaTypeKey;
        public string MediaTypeDisplayName => _MediaTypeDisplayName;
        public DateTime CreatedAt => _CreatedAt;
        public DateTime UpdatedAt => _UpdatedAt;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentItemListResponse
    {
        [JsonProperty("items")]
        [SerializeField]
        private List<ContentItemSummary> _Items;

        [JsonProperty("pagination")]
        [SerializeField]
        private PaginationMeta _Pagination;

        public List<ContentItemSummary> Items => _Items;
        public PaginationMeta Pagination => _Pagination;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class PaginationMeta
    {
        [JsonProperty("page")]
        [SerializeField]
        private int _Page;

        [JsonProperty("page_size")]
        [SerializeField]
        private int _PageSize;

        [JsonProperty("total_count")]
        [SerializeField]
        private int _TotalCount;

        [JsonProperty("has_more")]
        [SerializeField]
        private bool _HasMore;

        public int Page => _Page;
        public int PageSize => _PageSize;
        public int TotalCount => _TotalCount;
        public bool HasMore => _HasMore;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentItemCountResponse
    {
        [JsonProperty("total_count")]
        [SerializeField]
        private int _TotalCount;

        public int TotalCount => _TotalCount;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentMediaTypeResponse
    {
        [JsonProperty("key")]
        [SerializeField]
        private string _Key;

        [JsonProperty("display_name")]
        [SerializeField]
        private string _DisplayName;

        [JsonProperty("allowed_extensions")]
        [SerializeField]
        private List<string> _AllowedExtensions;

        [JsonProperty("max_size_bytes")]
        [SerializeField]
        private long _MaxSizeBytes;

        [JsonProperty("is_built_in")]
        [SerializeField]
        private bool _IsBuiltIn;

        [JsonProperty("is_disabled")]
        [SerializeField]
        private bool _IsDisabled;

        public string Key => _Key;
        public string DisplayName => _DisplayName;
        public List<string> AllowedExtensions => _AllowedExtensions;
        public long MaxSizeBytes => _MaxSizeBytes;
        public bool IsBuiltIn => _IsBuiltIn;
        public bool IsDisabled => _IsDisabled;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentMediaTypeListResponse
    {
        [JsonProperty("media_types")]
        [SerializeField]
        private List<ContentMediaTypeResponse> _MediaTypes;

        public List<ContentMediaTypeResponse> MediaTypes => _MediaTypes;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ContentShareResponse
    {
        [JsonProperty("token")]
        [SerializeField]
        private string _Token;

        [JsonProperty("url")]
        [SerializeField]
        private string _Url;

        [JsonProperty("item")]
        [SerializeField]
        private ContentItemSummary _Item;

        public string Token => _Token;
        public string Url => _Url;
        public ContentItemSummary Item => _Item;
    }
}
