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
        private const float REFRESH_TIME = 10;
        private static readonly HttpClient CLIENT = new();

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
                
            Debug.Log("[ServerCodeManager] ServerManager initialized");
            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }
        
        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                if (InstanceFinder.ServerManager.IsOnlyOneServerStarted()) 
                    RegisterServerByCode();
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                if (!InstanceFinder.ServerManager.IsAnyServerStarted())
                    DisposeServer();
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
        }

        private void DisposeServer()
        {
            Debug.Log("[ServerCodeManager] Disposing server registration");
            if (!string.IsNullOrEmpty(_serverSecret))
            {
                DisposeServer(new ServerDisposeRequest { secret = _serverSecret }).Forget();
            }
        }

        private void RegisterServerByCode()
        {
            RegisterServerByCodeAsync().Forget();
        }
        
        private async UniTaskVoid ServerRegistrationHeartbeat(ServerRefreshRequest request, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ServerRefreshRequest(request);
                await UniTask.WaitForSeconds(REFRESH_TIME, true, cancellationToken: cancellationToken);
            }
        }

        private async UniTask ServerRefreshRequest(ServerRefreshRequest req)
        {
            var serverManagerURL = GetServerManager();
            if (serverManagerURL == null) return;
            
            try
            {
                const string endpoint = "server/refresh";
                var response = await CLIENT.PostAsync(Path.Combine(serverManagerURL, endpoint), ToJsonContent(req));
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
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void RegisterServerAgain()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            RegisterServerByCode();
        }

        private async UniTaskVoid DisposeServer(ServerDisposeRequest req)
        {
            var serverManagerURL = GetServerManager();
            if (serverManagerURL == null) return;

            try
            {
                const string endpoint = "server/dispose";
                var response = await CLIENT.PostAsync(Path.Combine(serverManagerURL, endpoint), ToJsonContent(req));
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

        private async UniTaskVoid RegisterServerByCodeAsync()
        {
            try
            {
                Debug.Log("[ServerCodeManager] Registering server by code");
                var code = !string.IsNullOrEmpty(_Config.ServerCode) ? _Config.ServerCode : GetRandomCode(4);
                var port = InstanceFinder.TransportManager.Transport.GetPort();

                var req = new ServerRegisterRequest
                {
                    code = code,
                    org = "", // TODO
                    address = GetIp(),
                    port = port,
                    flavour = Application.productName,
                    version = Application.version,
                    time = $"{DateTime.Now:yyyy-MM-dd_HH:mm:ss}"
                };
            
                var serverManagerURL = GetServerManager();
                if (serverManagerURL == null) return;

                const string endpoint = "server/register";
                const string errorMessage = "Server code registration failed, but the server will still be accessible via its IP.";
                
                var response = await CLIENT.PostAsync(Path.Combine(serverManagerURL, endpoint), ToJsonContent(req));
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
        
        public async UniTaskVoid GetServerByCode(string org, string code, UnityAction<QueryServerResponse.Data> callback)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    Debug.LogError("No code provided.");
                    return;
                }

                var serverManagerURL = GetServerManager();
                if (serverManagerURL == null) return;

                var url = Path.Combine(serverManagerURL, $"query/server?org={org}&code={code}");
                Debug.Log("Request url: " + url);
                var response = await CLIENT.GetAsync(url);
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
        
        public static StringContent ToJsonContent(object o)
        {
            return new StringContent(o == null ? "{}" : JsonUtility.ToJson(o), Encoding.UTF8, "application/json");
        }

        public static async UniTask<T> FromJson<T>(HttpResponseMessage response, T definition)
        {
            if (response == null) return definition;
            var responseString = await response.Content.ReadAsStringAsync();
            if (responseString == null) return definition;
            return JsonUtility.FromJson<T>(responseString);
        }

        private string GetIp()
        {
            return _Config.RegisterLocalIP ? GetLocalIp() : GetExternalIp();
        }

        public static string GetExternalIp()
        {
            return new WebClient()
                .DownloadString("http://ipv4.icanhazip.com")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "");
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
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static string GetRandomCode(int lenght)
        {
            return new string(Enumerable.Repeat(CODE_CHARS, lenght).Select(s => s[UnityEngine.Random.Range(0, CODE_CHARS.Length)]).ToArray());
        }
    }
}
