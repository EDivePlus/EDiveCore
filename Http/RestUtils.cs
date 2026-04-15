using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace EDIVE.Http
{
    public static class RestUtils
    {
        private const int DEFAULT_TIMEOUT = 30;

        public static UniTask<NetworkResponse<TResponse>> PostAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<TResponse, TRequest>(url, UnityWebRequest.kHttpVerbPOST, request, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<NetworkResponse<TResponse>> PutAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<TResponse, TRequest>(url, UnityWebRequest.kHttpVerbPUT, request, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<NetworkResponse<TResponse>> PatchAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<TResponse, TRequest>(url, "PATCH", request, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<NetworkResponse<TResponse>> DeleteAsync<TResponse>(string url, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbDELETE, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<NetworkResponse<TResponse>> GetAsync<TResponse>(string url, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbGET, authToken, headers, timeout, cancellationToken);
        }

        private static async UniTask<NetworkResponse<TResponse>> SendRequestAsync<TResponse>(
            string url,
            string method,
            string jsonPayload,
            string authToken,
            Dictionary<string, string> headers,
            int timeout,
            CancellationToken cancellationToken)
        {
            var result = await SendRawRequestAsync(url, method, jsonPayload, authToken, headers, timeout, cancellationToken);
            
            if (!result.IsSuccess)
                return NetworkResponse<TResponse>.Error(result.StatusCode, result.ErrorMessage);

            if (typeof(TResponse) == typeof(string))
                return NetworkResponse<TResponse>.Success(result.StatusCode, (TResponse)(object) result.Raw);

            if (string.IsNullOrWhiteSpace(result.Raw))
                return NetworkResponse<TResponse>.Success(result.StatusCode, default);

            try
            {
                var deserialized = JsonConvert.DeserializeObject<TResponse>(result.Raw);
                return NetworkResponse<TResponse>.Success(result.StatusCode, deserialized);
            }
            catch (Exception ex)
            {
                return NetworkResponse<TResponse>.Error(result.StatusCode, $"Deserialization failed: {ex.Message}");
            }
        }

        private static async UniTask<NetworkResponse<TResponse>> SendRequestAsync<TResponse, TRequest>(
            string url, 
            string method, 
            TRequest payload,
            string authToken, 
            Dictionary<string, string> headers,
            int timeout,
            CancellationToken cancellationToken)
        {
            var jsonPayload = payload != null ? JsonConvert.SerializeObject(payload) : null;
            return await SendRequestAsync<TResponse>(url, method, jsonPayload, authToken, headers, timeout, cancellationToken);
        }
        
        private static async UniTask<NetworkResponse<TResponse>> SendRequestAsync<TResponse>(
            string url, 
            string method,
            string authToken, 
            Dictionary<string, string> headers,
            int timeout,
            CancellationToken cancellationToken)
        {
            return await SendRequestAsync<TResponse>(url, method, null, authToken, headers, timeout, cancellationToken);
        }

        private static async UniTask<RawNetworkResponse> SendRawRequestAsync(
            string url, 
            string method, 
            string jsonPayload, 
            string authToken = null, 
            Dictionary<string, string> headers = null, 
            int timeout = DEFAULT_TIMEOUT, 
            CancellationToken cancellationToken = default)
        {
            using var webRequest = new UnityWebRequest(url, method);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = timeout;

            if (!string.IsNullOrEmpty(jsonPayload))
            {
                var bytes = Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bytes);
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }

            webRequest.SetRequestHeader("Accept", "application/json");
    
            if (!string.IsNullOrEmpty(authToken)) 
                webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");

            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    webRequest.SetRequestHeader(kvp.Key, kvp.Value);
                }
            }

            try
            {
                await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                var rawResponse = webRequest.downloadHandler?.text ?? string.Empty;
                return RawNetworkResponse.Success(webRequest.responseCode, rawResponse);
            }
            catch (OperationCanceledException)
            {
                throw; 
            }
            catch (UnityWebRequestException ex)
            {
                return RawNetworkResponse.Error(ex.ResponseCode, ex.Message, ex.Text);
            }
            catch (Exception ex)
            {
                return RawNetworkResponse.Error(webRequest.responseCode, ex.Message, string.Empty);
            }
        }
    }
}