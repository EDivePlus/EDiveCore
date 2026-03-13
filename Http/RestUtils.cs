// Author: Michal Petr
// Created: 12.03.2026

using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace EDIVE.Http
{
    public static class RestUtils
    {
        private static async UniTask<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request)
        {
            var json = JsonConvert.SerializeObject(request);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(bytes)
            {
                contentType = "application/json"
            };
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Accept", "application/json");

            await webRequest.SendWebRequest();
            
            if (webRequest.result != UnityWebRequest.Result.Success)
                throw new Exception($"POST {url} failed: {webRequest.error} — {webRequest.downloadHandler.text}");

            return JsonConvert.DeserializeObject<TResponse>(webRequest.downloadHandler.text);
        }
        
        private static async UniTask<T> GetAsync<T>(string url)
        {
            using var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Accept", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"GET {url} failed: {request.error} — {request.downloadHandler.text}");

            return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
        }
    }
}
