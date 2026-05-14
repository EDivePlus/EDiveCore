// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Auth;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent
{
    public class RemoteContentService : MonoBehaviour, IServiceHubModule
    {
        private ServiceHubSettings _settings;

        private readonly Dictionary<string, RemoteContentResult> _remoteContentCache = new();
        private readonly object _remoteContentCacheLock = new();

        private string ContentBaseUrl => $"{_settings.ServiceBaseUrl}/content";
        private string ContentMediaTypesUrl => $"{ContentBaseUrl}/media-types";
        private string ContentItemsUrl => $"{ContentBaseUrl}/items";
        private string ContentItemsCountUrl => $"{ContentBaseUrl}/items/count";
        private string ItemShareUrl(string itemId) => $"{ContentBaseUrl}/items/{Uri.EscapeDataString(itemId)}/share";
        private string SharedContentUrl(string token) => $"{ContentBaseUrl}/shared/{Uri.EscapeDataString(token)}";
        private string SharedContentInfoUrl(string token) => $"{ContentBaseUrl}/shared/{Uri.EscapeDataString(token)}/info";

        private int RequestTimeoutSeconds => _settings.ApiTimeoutSeconds;

        public void Initialize(ServiceHubSettings settings)
        {
            _settings = settings;
        }

        private static string AppendQuery(string url, string key, string value)
        {
            var sep = url.Contains('?') ? '&' : '?';
            return $"{url}{sep}{key}={Uri.EscapeDataString(value)}";
        }

        public async UniTask<NetworkResponse<ContentItemInfo>> GetSharedContentInfoAsync(
            string shareToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(shareToken))
                return NetworkResponse<ContentItemInfo>.Error(0, "Share token is empty");

            var response = await RestUtils.GetAsync<ApiResponse<ContentItemInfo>>(
                SharedContentInfoUrl(shareToken),
                authToken: null,
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: cancellationToken
            );
            return ApiResponseHelper.UnwrapApi(response, $"GetSharedContentInfo({shareToken})");
        }

        public async UniTask<NetworkResponse<RemoteContentResult>> GetRemoteContentAsync(
            string shareToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(shareToken))
                return NetworkResponse<RemoteContentResult>.Error(0, "Share token is empty");

            lock (_remoteContentCacheLock)
            {
                if (_remoteContentCache.TryGetValue(shareToken, out var cached))
                    return NetworkResponse<RemoteContentResult>.Success(200, cached);
            }

            var response = await RestUtils.GetBytesAsync(
                SharedContentUrl(shareToken),
                authToken: null,
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            if (!response.IsSuccess || response.Result == null)
            {
                Debug.LogWarning($"[ServiceHub] Remote content fetch failed for token '{shareToken}': {response.ErrorMessage}");
                return NetworkResponse<RemoteContentResult>.Error(response.StatusCode, response.ErrorMessage);
            }

            var result = new RemoteContentResult(response.Result);
            lock (_remoteContentCacheLock)
            {
                _remoteContentCache[shareToken] = result;
            }
            return NetworkResponse<RemoteContentResult>.Success(response.StatusCode, result);
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("RemoteContent", Color = "@ColorTools.Pink", SpaceBefore = 8)]
        public async UniTask<NetworkResponse<ContentMediaTypeListResponse>> ListContentMediaTypesAsync(
            CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.Client.IsValid())
                return NetworkResponse<ContentMediaTypeListResponse>.Error(401, "Not authenticated");

            var response = await RestUtils.GetAsync<ApiResponse<ContentMediaTypeListResponse>>(
                ContentMediaTypesUrl,
                authToken: AuthStorage.Client.GetAccessToken(),
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: cancellationToken
            );
            return ApiResponseHelper.UnwrapApi(response, "ListContentMediaTypes");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("RemoteContent")]
        public async UniTask<NetworkResponse<ContentItemListResponse>> ListOwnContentItemsAsync(
            string mediaTypeKey = null,
            string search = null,
            int page = 0,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.Client.IsValid())
                return NetworkResponse<ContentItemListResponse>.Error(401, "Not authenticated");

            var url = ContentItemsUrl;
            if (!string.IsNullOrWhiteSpace(mediaTypeKey))
                url = AppendQuery(url, "mediaTypeKey", mediaTypeKey);
            if (!string.IsNullOrWhiteSpace(search))
                url = AppendQuery(url, "search", search);
            url = AppendQuery(url, "page", page.ToString(CultureInfo.InvariantCulture));
            url = AppendQuery(url, "pageSize", pageSize.ToString(CultureInfo.InvariantCulture));

            var response = await RestUtils.GetAsync<ApiResponse<ContentItemListResponse>>(
                url,
                authToken: AuthStorage.Client.GetAccessToken(),
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: cancellationToken
            );
            return ApiResponseHelper.UnwrapApi(response, "ListOwnContentItems");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("RemoteContent")]
        public async UniTask<NetworkResponse<ContentItemCountResponse>> CountOwnContentItemsAsync(
            string mediaTypeKey = null,
            string search = null,
            CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.Client.IsValid())
                return NetworkResponse<ContentItemCountResponse>.Error(401, "Not authenticated");

            var url = ContentItemsCountUrl;
            if (!string.IsNullOrWhiteSpace(mediaTypeKey))
                url = AppendQuery(url, "mediaTypeKey", mediaTypeKey);
            if (!string.IsNullOrWhiteSpace(search))
                url = AppendQuery(url, "search", search);

            var response = await RestUtils.GetAsync<ApiResponse<ContentItemCountResponse>>(
                url,
                authToken: AuthStorage.Client.GetAccessToken(),
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: cancellationToken
            );
            return ApiResponseHelper.UnwrapApi(response, "CountOwnContentItems");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("RemoteContent")]
        public async UniTask<NetworkResponse<ContentShareResponse>> CreateContentShareAsync(
            string itemId,
            CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.Client.IsValid())
                return NetworkResponse<ContentShareResponse>.Error(401, "Not authenticated");

            if (string.IsNullOrEmpty(itemId))
                return NetworkResponse<ContentShareResponse>.Error(0, "Content item id is empty");

            var response = await RestUtils.PostAsync<ApiResponse<ContentShareResponse>, object>(
                ItemShareUrl(itemId),
                request: null,
                authToken: AuthStorage.Client.GetAccessToken(),
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: cancellationToken
            );
            return ApiResponseHelper.UnwrapApi(response, $"CreateContentShare({itemId})");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("RemoteContent")]
        public async UniTask<NetworkResponse<bool>> RevokeContentShareAsync(
            string itemId,
            CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.Client.IsValid())
                return NetworkResponse<bool>.Error(401, "Not authenticated");

            if (string.IsNullOrEmpty(itemId))
                return NetworkResponse<bool>.Error(0, "Content item id is empty");

            var response = await RestUtils.DeleteAsync<ApiResponse<object>>(
                ItemShareUrl(itemId),
                authToken: AuthStorage.Client.GetAccessToken(),
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            if (response.IsNotFound)
                return NetworkResponse<bool>.Success(response.StatusCode, false);

            if (!response.IsSuccess || response.Result == null)
            {
                Debug.LogError($"[ServiceHub] RevokeContentShare({itemId}) failed: {response.ErrorMessage}");
                return NetworkResponse<bool>.Error(response.StatusCode, response.ErrorMessage);
            }

            if (response.Result.Status != 0)
            {
                Debug.LogError($"[ServiceHub] RevokeContentShare({itemId}) API error: {response.Result.Message}");
                return NetworkResponse<bool>.Error(response.StatusCode, response.Result.Message);
            }

            return NetworkResponse<bool>.Success(response.StatusCode, true);
        }
    }
}
