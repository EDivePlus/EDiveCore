using System;
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

        public static UniTask<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPOST, json, authToken, cancellationToken);
        }

        public static UniTask<TResponse> PutAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPUT, json, authToken, cancellationToken);
        }

        public static UniTask<TResponse> PatchAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            return SendRequestAsync<TResponse>(url, "PATCH", json, authToken, cancellationToken);
        }

        public static UniTask<T> DeleteAsync<T>(string url, string authToken = null, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, authToken, cancellationToken);
        }

        public static UniTask<T> GetAsync<T>(string url, string authToken = null, CancellationToken cancellationToken = default)
        {
            return SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, authToken, cancellationToken);
        }

        private static async UniTask<TResponse> SendRequestAsync<TResponse>(
            string url, 
            string method, 
            string jsonPayload, 
            string authToken, 
            CancellationToken cancellationToken)
        {
            using var webRequest = new UnityWebRequest(url, method);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = DEFAULT_TIMEOUT;

            if (jsonPayload != null)
            {
                var bytes = Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bytes);
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }

            webRequest.SetRequestHeader("Accept", "application/json");
    
            if (!string.IsNullOrEmpty(authToken)) 
                webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");

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

            var responseText = webRequest.downloadHandler?.text;

            if (string.IsNullOrWhiteSpace(responseText)) return default;

            return await UniTask.RunOnThreadPool(() => JsonConvert.DeserializeObject<TResponse>(responseText), cancellationToken: cancellationToken);
        }
    }
}