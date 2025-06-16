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
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.OdinExtensions.Attributes;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace EDIVE.MirrorNetworking.ServerCodes
{
    public class ServerCodeManager : ALoadableServiceBehaviour<ServerCodeManager>
    {
        [ShowCreateNew]
        [SerializeField]
        private ServerCodeConfig _Config;

        [Sirenix.OdinInspector.ShowInInspector]
        private string _registeredWithCode;

        private IEnumerator _serverRefreshCoroutine;
        private string _serverSecret;

        private const string CODE_CHARS = "AB0123456789";
        private const float REFRESH_TIME = 10;
        private static readonly HttpClient CLIENT = new();

        private MasterNetworkManager _networkManager;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = AppCore.Services.Get<MasterNetworkManager>();
            if (_Config.AutoRegisterServer)
                _networkManager.ServerStarted.AddListener(RegisterServerByCode);
            _networkManager.ServerStopped.AddListener(DisposeServer);

            return UniTask.CompletedTask;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (AppCore.Services.TryGet(out _networkManager))
            {
                _networkManager.ServerStarted.RemoveListener(RegisterServerByCode);
                _networkManager.ServerStopped.RemoveListener(DisposeServer);
            }
        }

        private IEnumerator LogServerCode()
        {
            yield return new WaitForSecondsRealtime(5);
            Debug.Log($"The server code is {_registeredWithCode}");
        }

        private void DisposeServer()
        {
            if (!string.IsNullOrEmpty(_serverSecret))
            {
                DisposeServer(new ServerDisposeRequest { secret = _serverSecret }).Forget();
            }
        }

        private void RegisterServerByCode()
        {
            var code = !string.IsNullOrEmpty(_Config.ServerCode) ? _Config.ServerCode : GetRandomCode(4);
            var port = (ushort) (_networkManager.transport is PortTransport portTransport ? portTransport.Port : 8080);
            RegisterServerByCode(
                new ServerRegisterRequest
                {
                    code = code,
                    org = "", // TODO
                    address = GetIp(),
                    port = port,
                    flavour = Application.productName,
                    version = Application.version,
                    time = $"{DateTime.Now:yyyy-MM-dd_HH:mm:ss}"
                },
                RegisterServerByCode_Callback).Forget();
        }

        private void RegisterServerByCode_Callback(ServerRegisterResponse.Data response)
        {
            Debug.Log($"Server registered with code {response.code}");

            _registeredWithCode = response.code;
            _serverSecret = response.secret;
            _serverRefreshCoroutine = ServerRegistrationRefresh(new ServerRefreshRequest
                {
                    title = _Config.ServerTitle,
                    secret = _serverSecret
                });
            StartCoroutine(_serverRefreshCoroutine);

            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(5));
                Debug.Log($"The server code is {_registeredWithCode}");
            });
        }

        private IEnumerator ServerRegistrationRefresh(ServerRefreshRequest request)
        {
            while (true)
            {
                ServerRefreshRequest(request).Forget();;
                yield return new WaitForSecondsRealtime(REFRESH_TIME);
            }
        }

        private async UniTask ServerRefreshRequest(ServerRefreshRequest req)
        {
            var serverManagerURL = GetServerManager();
            if (serverManagerURL == null) return;

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
                return;
            }
        }

        private void RegisterServerAgain()
        {
            // if the coroutine was already running, stop it first (can happen when the server needs to register again)
            if (_serverRefreshCoroutine != null) StopCoroutine(_serverRefreshCoroutine);
            RegisterServerByCode();
        }

        private async UniTaskVoid DisposeServer(ServerDisposeRequest req)
        {
            var serverManagerURL = GetServerManager();
            if (serverManagerURL == null) return;

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
                return;
            }
        }

        private async UniTaskVoid RegisterServerByCode(ServerRegisterRequest req, UnityAction<ServerRegisterResponse.Data> callback)
        {
            var serverManagerURL = GetServerManager();
            if (serverManagerURL == null) return;

            const string endpoint = "server/register";
            const string errorMessage = "Server code registration failed, but the server will still be accessible via its IP.";

            HttpResponseMessage response = null;
            try
            {
                response = await CLIENT.PostAsync(Path.Combine(serverManagerURL, endpoint), ToJsonContent(req));
            }
            catch (Exception e)
            {
                PrintErrorMsg(errorMessage, e.Message);
                return;
            }
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

            try
            {
                callback.Invoke(responseObject.data);
            }
            catch (Exception)
            {
                Debug.LogWarning("callback does not exist");
            }
        }

        private static void PrintErrorMsg(string errorMessage, string error)
        {
            Debug.LogError($"{errorMessage} Error '{error}'.");
        }

        // TODO move these to a specific class only for managing the codes?
        // -- also generalize the methods? (all have status, message and data)
        // ...but that would probably not possible right now since structs...so change them to classes?
        public async UniTaskVoid GetServerByCode(string org, string code, UnityAction<QueryServerResponse.Data> callback)
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
                // TODO some UI info
                return;
            }

            try
            {
                callback.Invoke(responseObject.data);
            }
            catch (Exception)
            {
                Debug.LogWarning("callback does not exist");
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
