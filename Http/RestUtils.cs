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
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPOST, json, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<NetworkResponse<TResponse>> PutAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPUT, json, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<NetworkResponse<TResponse>> PatchAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, "PATCH", json, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<NetworkResponse<T>> DeleteAsync<T>(string url, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<NetworkResponse<T>> GetAsync<T>(string url, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, authToken, headers, timeout, cancellationToken);
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

            if (!result.Success)
                return NetworkResponse<TResponse>.Fail(result.StatusCode, result.Error, result.Raw);

            if (typeof(TResponse) == typeof(string))
                return NetworkResponse<TResponse>.Ok(result.StatusCode, (TResponse)(object) result.Raw, result.Raw);

            if (string.IsNullOrWhiteSpace(result.Raw))
                return NetworkResponse<TResponse>.Ok(result.StatusCode, default, result.Raw);

            try
            {
                var deserialized = await UniTask.RunOnThreadPool(() => JsonConvert.DeserializeObject<TResponse>(result.Raw), cancellationToken: cancellationToken);
                return NetworkResponse<TResponse>.Ok(result.StatusCode, deserialized, result.Raw);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return NetworkResponse<TResponse>.Fail(result.StatusCode, $"Deserialization failed: {ex.Message}", result.Raw);
            }
        }

        private static async UniTask<NetworkResponse<string>> SendRawRequestAsync(
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
                return NetworkResponse<string>.Ok(webRequest.responseCode, rawResponse, rawResponse);
            }
            catch (OperationCanceledException)
            {
                throw; 
            }
            catch (UnityWebRequestException ex)
            {
                return NetworkResponse<string>.Fail(ex.ResponseCode, ex.Message, ex.Text);
            }
            catch (Exception ex)
            {
                return NetworkResponse<string>.Fail(webRequest.responseCode, ex.Message, string.Empty);
            }
        }
    }
}