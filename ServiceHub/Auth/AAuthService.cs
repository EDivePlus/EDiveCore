// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.Auth
{
    public abstract class AAuthService<TLoginResponse> : MonoBehaviour, IServiceHubModule
        where TLoginResponse : class
    {
        protected ServiceHubSettings Settings { get; private set; }

        public event Action<TLoginResponse> OnLoginSucceeded;
        public event Action<long, string> OnLoginFailed;
        public event Func<CancellationToken, UniTask> OnLoggingOutAsync;
        public event Action OnLoggedOut;

        public bool IsLoggedIn => Storage.IsValid();

        protected abstract AuthStorage Storage { get; }
        protected abstract string LogScope { get; }
        protected abstract void SaveLoginToStorage(TLoginResponse response);

        public virtual void Initialize(ServiceHubSettings settings)
        {
            Settings = settings;
        }

        public async UniTask LogoutAsync(CancellationToken cancellationToken = default)
        {
            var effectiveToken = cancellationToken == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : cancellationToken;
            
            if (OnLoggingOutAsync != null)
            {
                var tasks = OnLoggingOutAsync.GetInvocationList()
                    .Cast<Func<CancellationToken, UniTask>>()
                    .Where(h => h != null)
                    .Select(h => h(effectiveToken));

                await UniTask.WhenAll(tasks);
            }

            Storage.Clear();
            OnLoggedOut?.Invoke();
        }

        protected NetworkResponse<TLoginResponse> HandleLoginResponse(NetworkResponse<ApiResponse<TLoginResponse>> response)
        {
            if (!response.IsSuccess && response.Result is null)
            {
                Debug.LogError($"[ServiceHub] {LogScope} request failed: {response.ErrorMessage}");
                OnLoginFailed?.Invoke(response.StatusCode, response.ErrorMessage);
                return NetworkResponse<TLoginResponse>.Error(response.StatusCode, response.ErrorMessage);
            }

            var apiResponse = response.Result;

            if (apiResponse == null || apiResponse.Status != 0 || apiResponse.Data == null)
            {
                var message = apiResponse?.Message ?? "Unknown error";
                var statusCode = apiResponse?.Status ?? -1;
                Debug.LogError($"[ServiceHub] {LogScope} failed ({statusCode}): {message}");
                OnLoginFailed?.Invoke(statusCode, message);
                return NetworkResponse<TLoginResponse>.Error(response.StatusCode, message);
            }

            var data = apiResponse.Data;
            SaveLoginToStorage(data);

            Debug.Log($"[ServiceHub] {LogScope} successful.");
            OnLoginSucceeded?.Invoke(data);

            var result = NetworkResponse<TLoginResponse>.Success(response.StatusCode, data);
            Debug.Log($"[ServiceHub] {LogScope} response (formatted): {JsonConvert.SerializeObject(result, Formatting.Indented)}");
            return result;
        }
    }
}
