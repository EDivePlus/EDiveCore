using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace EDIVE.Http
{
    public class RestRequestException : Exception
    {
        public long StatusCode { get; }
        public string Response { get; }

        public RestRequestException(string message, long statusCode, string response) : base(message)
        {
            StatusCode = statusCode;
            Response = response;
        }
    }

    public static class RestUtils
    {
        private const int DEFAULT_TIMEOUT = 30;

        public static UniTask<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPOST, json, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<TResponse> PutAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPUT, json, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<TResponse> PatchAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, "PATCH", json, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<T> DeleteAsync<T>(string url, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, authToken, headers, timeout, cancellationToken);
        }

        public static UniTask<T> GetAsync<T>(string url, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, authToken, headers, timeout, cancellationToken);
        }

        private static async UniTask<TResponse> SendRequestAsync<TResponse>(
            string url, 
            string method, 
            string jsonPayload, 
            string authToken, 
            Dictionary<string, string> headers,
            int timeout,
            CancellationToken cancellationToken)
        {
            var result = await SendRawRequestAsync(url, method, jsonPayload, authToken, headers, timeout, cancellationToken);

            if (string.IsNullOrWhiteSpace(result.response)) return default;

            return await UniTask.RunOnThreadPool(() => JsonConvert.DeserializeObject<TResponse>(result.response), cancellationToken: cancellationToken);
        }

        public static async UniTask<(long statusCode, string response)> SendRawRequestAsync(
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
            }
            catch (UnityWebRequestException ex)
            {
                throw new RestRequestException(
                    $"{method} {url} failed: {ex.Message}", 
                    ex.ResponseCode, 
                    ex.Text
                );
            }

            return (webRequest.responseCode, webRequest.downloadHandler?.text ?? string.Empty);
        }
    }
}