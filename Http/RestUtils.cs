// Author: Michal Petr
// Created: 12.03.2026

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
        private const int DEFAULT_TIMEOUT = 30; // Seconds

        public static async UniTask<TResponse> PostAsync<TResponse, TRequest>(
            string url, 
            TRequest request, 
            string authToken = null,
            CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(bytes)
            {
                contentType = "application/json"
            };
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = DEFAULT_TIMEOUT;
            
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Accept", "application/json");
            
            if (!string.IsNullOrEmpty(authToken))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

            // Pass cancellation token to SendWebRequest
            await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new RestRequestException(
                    $"POST {url} failed: {webRequest.error}", 
                    webRequest.responseCode, 
                    webRequest.downloadHandler.text
                );
            }

            return JsonConvert.DeserializeObject<TResponse>(webRequest.downloadHandler.text);
        }

        public static async UniTask<TResponse> PutAsync<TResponse, TRequest>(
            string url, 
            TRequest request, 
            string authToken = null,
            CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
            webRequest.uploadHandler = new UploadHandlerRaw(bytes)
            {
                contentType = "application/json"
            };
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = DEFAULT_TIMEOUT;
            
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Accept", "application/json");
            
            if (!string.IsNullOrEmpty(authToken))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

            // Pass cancellation token to SendWebRequest
            await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new RestRequestException(
                    $"PUT {url} failed: {webRequest.error}", 
                    webRequest.responseCode, 
                    webRequest.downloadHandler.text
                );
            }

            return JsonConvert.DeserializeObject<TResponse>(webRequest.downloadHandler.text);
        }

        public static async UniTask<TResponse> PatchAsync<TResponse, TRequest>(
            string url, 
            TRequest request, 
            string authToken = null,
            CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(request);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var webRequest = new UnityWebRequest(url, "PATCH");
            webRequest.uploadHandler = new UploadHandlerRaw(bytes)
            {
                contentType = "application/json"
            };
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = DEFAULT_TIMEOUT;
            
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Accept", "application/json");
            
            if (!string.IsNullOrEmpty(authToken))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

            // Pass cancellation token to SendWebRequest
            await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new RestRequestException(
                    $"PATCH {url} failed: {webRequest.error}", 
                    webRequest.responseCode, 
                    webRequest.downloadHandler.text
                );
            }

            return JsonConvert.DeserializeObject<TResponse>(webRequest.downloadHandler.text);
        }

        public static async UniTask<T> DeleteAsync<T>(
            string url, 
            string authToken = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Delete(url);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = DEFAULT_TIMEOUT;
            webRequest.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrEmpty(authToken))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

            await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new RestRequestException(
                    $"DELETE {url} failed: {webRequest.error}", 
                    webRequest.responseCode, 
                    webRequest.downloadHandler.text
                );
            }

            return JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
        }

        public static async UniTask<T> GetAsync<T>(
            string url, 
            string authToken = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Get(url);
            webRequest.timeout = DEFAULT_TIMEOUT;
            webRequest.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrEmpty(authToken))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

            await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new RestRequestException(
                    $"GET {url} failed: {webRequest.error}", 
                    webRequest.responseCode, 
                    webRequest.downloadHandler.text
                );
            }

            return JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
        }
    }
}
