// Author Vojtech Bruza

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.OdinExtensions.Attributes;
using FishNet;
using FishNet.Transporting;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace EDIVE.Networking.ServerCodes
{
    public class ServerCodeManager : ALoadableServiceBehaviour<ServerCodeManager>
    {
        [ShowCreateNew]
        [SerializeField]
        private ServerCodeConfig _Config;

        [ReadOnly]
        [ShowInInspector]
        public string RegisteredWithCode { get; private set; }

        private CancellationTokenSource _heartbeatCts;
        private string _serverSecret;

        private const string CODE_CHARS = "AB0123456789";
        private const float HEARTBEAT_INTERVAL = 10;
        private readonly HttpClient _client = new();

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            if (!_Config.AutoRegisterServer)
            {
                Debug.Log("[ServerCodeManager] Auto registration disabled");
                return;
            }

            await AppCore.Services.AwaitRegistered<MasterNetworkManager>();
            await UniTask.Yield();

            if (InstanceFinder.ServerManager == null)
            {
                Debug.Log("[ServerCodeManager] ServerManager not found");
                return;
            }

            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                if (InstanceFinder.ServerManager.IsOnlyOneServerStarted())
                    RegisterServerByCodeAsync().Forget();
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                if (!InstanceFinder.ServerManager.IsAnyServerStarted())
                    DisposeServerAsync().Forget();
            }
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            }
            
            _client?.Dispose();
        }
        

        private async UniTask ServerRegistrationHeartbeat(ServerRefreshRequest request, CancellationToken cancellationToken)
        {
            var serverManagerURL = GetServerManager();
            if (serverManagerURL == null) return;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    const string endpoint = "server/refresh";
                    var response = await _client.PostAsync(CombineUrl(serverManagerURL, endpoint), ToJsonContent(request), cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogError($"Server refresh failed: '{response.StatusCode}'.");
                        RegisterServerAgain();
                        return;
                    }

                    var responseObject = await FromJson(response, new ServerRefreshResponse());
                    if (responseObject.status != 0)
                    {
                        Debug.LogError($"Server refresh failed: '{responseObject.message}'. Trying to register server again.");
                        RegisterServerAgain();
                    }
                    
                    await UniTask.WaitForSeconds(HEARTBEAT_INTERVAL, true, cancellationToken: cancellationToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Debug.LogException(e);
                    await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
                }
            }
        }

        private void RegisterServerAgain()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
            RegisterServerByCodeAsync().Forget();
        }

        private async UniTask DisposeServerAsync()
        {
            if (string.IsNullOrEmpty(_serverSecret))
                return;
            
            Debug.Log("[ServerCodeManager] Disposing server registration");
            var serverManagerURL = GetServerManager();
            if (serverManagerURL == null)
                return;
            
            try
            {
                var req = new ServerDisposeRequest {secret = _serverSecret};
                const string endpoint = "server/dispose";
                var response = await _client.PostAsync(CombineUrl(serverManagerURL, endpoint), ToJsonContent(req));
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"Server dispose failed: '{response.StatusCode}'.");
                    return;
                }
                var responseObject = await FromJson(response, new ServerDisposeResponse());

                if (responseObject.status != 0)
                {
                    Debug.LogError($"Server dispose failed: '{responseObject.message}'. Will still be disposed after a minute or so.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async UniTask RegisterServerByCodeAsync()
        {
            try
            {
                Debug.Log("[ServerCodeManager] Registering server by code");
                var code = !string.IsNullOrEmpty(_Config.ServerCode) ? _Config.ServerCode : GetRandomCode(4);
                var port = InstanceFinder.TransportManager.Transport.GetPort();
                var ip = _Config.RegisterLocalIP ? GetLocalIp() : await GetExternalIp();

                var req = new ServerRegisterRequest
                {
                    code = code,
                    org = "", // TODO
                    address = ip,
                    port = port,
                    flavour = Application.productName,
                    version = Application.version,
                    time = $"{DateTime.Now:yyyy-MM-dd_HH:mm:ss}"
                };
            
                var serverManagerURL = GetServerManager();
                if (serverManagerURL == null) return;

                const string endpoint = "server/register";
                const string errorMessage = "Server code registration failed, but the server will still be accessible via its IP.";
                
                var response = await _client.PostAsync(CombineUrl(serverManagerURL, endpoint), ToJsonContent(req));
                if (!response.IsSuccessStatusCode)
                {
                    PrintErrorMsg(errorMessage, response.StatusCode.ToString());
                    return;
                }
                var responseObject = await FromJson(response, new ServerRegisterResponse());
                if (responseObject.status != 0)
                {
                    PrintErrorMsg(errorMessage, responseObject.message);
                    return;
                }
                
                var responseData = responseObject.data;
                DebugLite.Log($"[ServerCodeManager] Server registered with code {responseData.code}");

                RegisteredWithCode = responseData.code;
                _serverSecret = responseData.secret;
                
                _heartbeatCts?.Cancel();
                _heartbeatCts?.Dispose();
                _heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                ServerRegistrationHeartbeat(new ServerRefreshRequest
                {
                    title = _Config.ServerTitle,
                    secret = _serverSecret
                }, _heartbeatCts.Token).Forget();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void PrintErrorMsg(string errorMessage, string error)
        {
            Debug.LogError($"{errorMessage} Error '{error}'.");
        }
        
        public async UniTask GetServerByCode(string org, string code, UnityAction<QueryServerResponse.Data> callback)
        {
            if (string.IsNullOrEmpty(code))
                return;

            var serverManagerURL = GetServerManager();
            if (serverManagerURL == null) 
                return;
            
            var url = CombineUrl(serverManagerURL, $"query/server?org={org}&code={code}");
            if (url == null) 
                return;
            
            try
            {
                Debug.Log("Request url: " + url);
                var response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"Client request failed for server from code failed: '{response.StatusCode}'.");
                    return;
                }
                var responseObject = await FromJson(response, new QueryServerResponse());
                if (responseObject.status != 0)
                {
                    Debug.LogError($"Client request for server from code failed: '{responseObject.message}'");
                    return;
                }
                callback?.Invoke(responseObject.data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private string GetServerManager()
        {
            var serverManagerURL = _Config.ServerManagerUrl;
            if (string.IsNullOrWhiteSpace(serverManagerURL))
            {
                Debug.LogError("The app setup is missing (no server manager URL).");
                return null;
            }
            return serverManagerURL;
        }
        
        private string CombineUrl(string baseUrl, string endpoint)
        {
            if (baseUrl == null) return null;
            return $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        
        public static StringContent ToJsonContent(object o)
        {
            return new StringContent(o == null ? "{}" : JsonUtility.ToJson(o), Encoding.UTF8, "application/json");
        }

        public static async UniTask<T> FromJson<T>(HttpResponseMessage response, T definition)
        {
            try
            {
                var text = await response.Content.ReadAsStringAsync();
                return string.IsNullOrWhiteSpace(text) ? default : JsonUtility.FromJson<T>(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON parse failed: {e}");
                return default;
            }
        }

        public async UniTask<string> GetExternalIp()
        {
            try
            {
                var s = await _client.GetStringAsync("http://ipv4.icanhazip.com");
                return new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return "0.0.0.0";
            }
        }

        public static string GetLocalIp()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList.Reverse()) // Need to take the last adapter
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            Debug.LogError("No IPv4 adapter found!");
            return "0.0.0.0";
        }

        private static string GetRandomCode(int length)
        {
            return new string(Enumerable.Range(0, length).Select(_ => CODE_CHARS[UnityEngine.Random.Range(0, CODE_CHARS.Length)]).ToArray());
        }
    }
}
