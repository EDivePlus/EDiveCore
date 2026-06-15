using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.External.DomainReloadHelper;
using EDIVE.Utils.Json;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace EDIVE.Http
{
    public static class RestUtils
    {
        private const int DEFAULT_TIMEOUT = 30;
        private static long _nextRequestId;

        [ClearOnReload] public static event Action<RequestStartedEvent> OnRequestStarted;
        [ClearOnReload] public static event Action<RequestCompletedEvent> OnRequestCompleted;
        [ClearOnReload] public static event Action<RequestCancelledEvent> OnRequestCancelled;

        public static UniTask<NetworkResponse<TResponse>> PostAsync<TResponse, TRequest>(string url, TRequest request, string authToken = null, Dictionary<string, string> headers = null, int timeout = DEFAULT_TIMEOUT, CancellationToken cancellationToken = default, CertificateHandler certificateHandler = null)
        {
            return SendRequestAsync<TResponse, TRequest>(url, UnityWebRequest.kHttpVerbPOST, request, authToken, headers, timeout, cancellationToken, certificateHandler);
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

        public static async UniTask<NetworkResponse<byte[]>> GetBytesAsync(
            string url,
            string authToken = null,
            Dictionary<string, string> headers = null,
            int timeout = DEFAULT_TIMEOUT,
            CancellationToken cancellationToken = default)
        {
            var requestId = _nextRequestId++;
            const string method = UnityWebRequest.kHttpVerbGET;

            if (OnRequestStarted != null)
            {
                var requestHeaders = new Dictionary<string, string> { ["Accept"] = "*/*" };
                if (!string.IsNullOrEmpty(authToken))
                    requestHeaders["Authorization"] = $"Bearer {authToken}";
                if (headers != null)
                {
                    foreach (var kvp in headers)
                        requestHeaders[kvp.Key] = kvp.Value;
                }

                OnRequestStarted.Invoke(new RequestStartedEvent(requestId, method, url, null, requestHeaders));
            }

            using var webRequest = new UnityWebRequest(url, method);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = timeout;
            webRequest.SetRequestHeader("Accept", "*/*");

            if (!string.IsNullOrEmpty(authToken))
                webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");

            if (headers != null)
            {
                foreach (var kvp in headers)
                    webRequest.SetRequestHeader(kvp.Key, kvp.Value);
            }

            try
            {
                await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                var data = webRequest.downloadHandler?.data ?? Array.Empty<byte>();
                OnRequestCompleted?.Invoke(new RequestCompletedEvent(requestId, webRequest.responseCode, true, $"<{data.Length} bytes>", null, webRequest.GetResponseHeaders()));
                return NetworkResponse<byte[]>.Success(webRequest.responseCode, data);
            }
            catch (OperationCanceledException)
            {
                OnRequestCancelled?.Invoke(new RequestCancelledEvent(requestId));
                throw;
            }
            catch (UnityWebRequestException ex)
            {
                OnRequestCompleted?.Invoke(new RequestCompletedEvent(requestId, ex.ResponseCode, false, ex.Text, ex.Message, webRequest.GetResponseHeaders()));
                return NetworkResponse<byte[]>.Error(ex.ResponseCode, ex.Message);
            }
            catch (Exception ex)
            {
                OnRequestCompleted?.Invoke(new RequestCompletedEvent(requestId, webRequest.responseCode, false, string.Empty, ex.Message, webRequest.GetResponseHeaders()));
                return NetworkResponse<byte[]>.Error(webRequest.responseCode, ex.Message);
            }
        }

        private static async UniTask<NetworkResponse<TResponse>> SendRequestAsync<TResponse>(
            string url,
            string method,
            string jsonPayload,
            string authToken,
            Dictionary<string, string> headers,
            int timeout,
            CancellationToken cancellationToken,
            CertificateHandler certificateHandler = null)
        {
            var result = await SendRawRequestAsync(url, method, jsonPayload, authToken, headers, timeout, cancellationToken, certificateHandler);

            if (!result.IsSuccess)
            {
                var errorObj = JsonUtils.TryDeserializeObject<TResponse>(result.Raw, out var response, out _);
                return NetworkResponse<TResponse>.Error(result.StatusCode, result.ErrorMessage, errorObj ? response : default);
            }

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
            CancellationToken cancellationToken,
            CertificateHandler certificateHandler = null)
        {
            var jsonPayload = payload != null ? JsonConvert.SerializeObject(payload) : null;
            return await SendRequestAsync<TResponse>(url, method, jsonPayload, authToken, headers, timeout, cancellationToken, certificateHandler);
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
            CancellationToken cancellationToken = default,
            CertificateHandler certificateHandler = null)
        {
            var requestId = _nextRequestId++;

            if (OnRequestStarted != null)
            {
                var requestHeaders = new Dictionary<string, string> { ["Accept"] = "application/json" };
                if (!string.IsNullOrEmpty(jsonPayload))
                    requestHeaders["Content-Type"] = "application/json";
                if (!string.IsNullOrEmpty(authToken))
                    requestHeaders["Authorization"] = $"Bearer {authToken}";
                if (headers != null)
                {
                    foreach (var kvp in headers)
                        requestHeaders[kvp.Key] = kvp.Value;
                }

                OnRequestStarted.Invoke(new RequestStartedEvent(requestId, method, url, jsonPayload, requestHeaders));
            }

            using var webRequest = new UnityWebRequest(url, method);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = timeout;

            if (certificateHandler != null)
                webRequest.certificateHandler = certificateHandler;

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
                var result = RawNetworkResponse.Success(webRequest.responseCode, rawResponse);
                OnRequestCompleted?.Invoke(new RequestCompletedEvent(requestId, webRequest.responseCode, true, rawResponse, null, webRequest.GetResponseHeaders()));
                return result;
            }
            catch (OperationCanceledException)
            {
                OnRequestCancelled?.Invoke(new RequestCancelledEvent(requestId));
                throw; 
            }
            catch (UnityWebRequestException ex)
            {
                OnRequestCompleted?.Invoke(new RequestCompletedEvent(requestId, ex.ResponseCode, false, ex.Text, ex.Message, webRequest.GetResponseHeaders()));
                return RawNetworkResponse.Error(ex.ResponseCode, ex.Message, ex.Text);
            }
            catch (Exception ex)
            {
                OnRequestCompleted?.Invoke(new RequestCompletedEvent(requestId, webRequest.responseCode, false, string.Empty, ex.Message, webRequest.GetResponseHeaders()));
                return RawNetworkResponse.Error(webRequest.responseCode, ex.Message, string.Empty);
            }
        }
    }
}