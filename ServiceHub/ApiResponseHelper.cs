// Author: Michal Petr
// Created: 12.05.2026

using EDIVE.Http;
using UnityEngine;

namespace EDIVE.ServiceHub
{
    internal static class ApiResponseHelper
    {
        public static NetworkResponse<T> UnwrapApi<T>(NetworkResponse<ApiResponse<T>> response, string scope)
        {
            if (!response.IsSuccess && response.Result == null)
            {
                Debug.LogError($"[ServiceHub] {scope} request failed: {response.ErrorMessage}");
                return NetworkResponse<T>.Error(response.StatusCode, response.ErrorMessage);
            }

            var api = response.Result;
            if (api == null || api.Status != 0 || api.Data == null)
            {
                var message = api?.Message ?? "Unknown error";
                var status = api?.Status ?? -1;
                Debug.LogError($"[ServiceHub] {scope} API error ({status}): {message}");
                return NetworkResponse<T>.Error(response.StatusCode, message, apiStatus: status);
            }

            return NetworkResponse<T>.Success(response.StatusCode, api.Data);
        }
    }
}
