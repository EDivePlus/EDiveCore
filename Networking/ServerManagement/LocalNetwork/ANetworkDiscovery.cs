using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Transporting;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.LocalNetwork
{
    public abstract class ANetworkDiscovery<TResponse> : MonoBehaviour
    {
        [SerializeField] 
        private string _Secret = "default_secret";
        
        [SerializeField] 
        private ushort _Port = 47777;
        
        [SerializeField] 
        private bool _Automatic = true;
        
        [SerializeField, Tooltip("How long in seconds to wait for a server response after sending a discovery packet.")]
        private float _SearchTimeout = 2f;
        
        [SerializeField, Tooltip("Interval in seconds between active discovery packets.")]
        private float _ActiveDiscoveryInterval = 2f;
        
        [SerializeField, Tooltip("How long in seconds before a server is removed if no response is received.")]
        private float _ServerTimeout = 10f;

        private NetworkManager _networkManager;
        private byte[] _secretBytes;
        private SynchronizationContext _mainThreadSynchronizationContext;
        private CancellationTokenSource _cancellationTokenSource;

        public bool IsAdvertising { get; private set; }
        public bool IsSearching { get; private set; }
        
        private readonly Dictionary<IPEndPoint, (TResponse response, float lastSeen)> _serverList = new();
        
        public IEnumerable<(IPEndPoint endPoint, TResponse response)> ServerList
        {
            get
            {
                lock (_serverList)
                {
                    return _serverList.Select(s => (s.Key, s.Value.response)).ToList();
                }
            }
        }
        
        public event Action ServerListUpdated;

        private float SearchTimeout => Mathf.Max(1.0f, _SearchTimeout);

        private void Awake()
        {
            _networkManager = GetComponentInParent<NetworkManager>() ?? InstanceFinder.NetworkManager;
            if (_networkManager == null)
            {
                NetworkManagerExtensions.LogWarning($"NetworkDiscovery on {gameObject.name} cannot work as NetworkManager wasn't found.");
                return;
            }

            _secretBytes = Encoding.UTF8.GetBytes(_Secret);
            _mainThreadSynchronizationContext = SynchronizationContext.Current;
            if (_networkManager.IsServerStarted) 
                AdvertiseServer();

            if (_networkManager.IsOffline) 
                SearchForServers();
        }

        private void OnEnable()
        {
            if (!_Automatic) return;

            _networkManager.ServerManager.OnServerConnectionState += ServerConnectionStateChangedEventHandler;
            _networkManager.ClientManager.OnClientConnectionState += ClientConnectionStateChangedEventHandler;
        }

        private void OnDisable() => Shutdown();
        private void OnDestroy() => Shutdown();
        private void OnApplicationQuit() => Shutdown();

        private void Shutdown()
        {
            if (_networkManager != null)
            {
                _networkManager.ServerManager.OnServerConnectionState -= ServerConnectionStateChangedEventHandler;
                _networkManager.ClientManager.OnClientConnectionState -= ClientConnectionStateChangedEventHandler;
            }

            StopSearchingOrAdvertising();
        }

        private void ServerConnectionStateChangedEventHandler(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
                AdvertiseServer();
            else if (args.ConnectionState == LocalConnectionState.Stopped)
                StopSearchingOrAdvertising();
        }

        private void ClientConnectionStateChangedEventHandler(ClientConnectionStateArgs args)
        {
            if (_networkManager.IsServerStarted) return;

            if (args.ConnectionState == LocalConnectionState.Started)
                StopSearchingOrAdvertising();
            else if (args.ConnectionState == LocalConnectionState.Stopped)
                SearchForServers();
        }

        public void AdvertiseServer()
        {
            if (IsAdvertising)
            {
                LogWarning("Server is already being advertised.");
                return;
            }
            
            StopSearchingOrAdvertising(); 
            _cancellationTokenSource = new CancellationTokenSource();
            AdvertiseServerAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        public void SearchForServers()
        {
            if (IsSearching)
            {
                LogWarning("Already searching for servers.");
                return;
            }

            StopSearchingOrAdvertising(); 
            _cancellationTokenSource = new CancellationTokenSource();
            SearchForServersAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        public void StopSearchingOrAdvertising()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private static byte[] SerializeResponse(TResponse response)
        {
            var json = JsonConvert.SerializeObject(response);
            return Encoding.UTF8.GetBytes(json);
        }

        private static TResponse DeserializeResponse(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<TResponse>(json);
        }

        private async Task AdvertiseServerAsync(CancellationToken cancellationToken)
        {
            UdpClient udpClient = null;
            try
            {
                LogInformation("Started advertising server.");
                IsAdvertising = true;
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    udpClient ??= new UdpClient(_Port)
                    {
                        EnableBroadcast = true,
                        MulticastLoopback = false
                    };
                    
                    var receiveTask = udpClient.ReceiveAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(SearchTimeout), cancellationToken);
                    var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                    if (completedTask == receiveTask)
                    {
                        var result = receiveTask.Result;
                        var receivedSecret = Encoding.UTF8.GetString(result.Buffer);

                        if (receivedSecret == _Secret)
                        {
                            var response = ProcessRequest(result.RemoteEndPoint);
                            var bytes = SerializeResponse(response);
                            await udpClient.SendAsync(bytes, bytes.Length, result.RemoteEndPoint);
                        }
                    }
                    else
                    {
                        udpClient.Close();
                        udpClient = null;
                    }
                }
                LogInformation("Stopped advertising server.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                IsAdvertising = false;
                udpClient?.Close();
            }
        }

        protected abstract TResponse ProcessRequest(IPEndPoint endpoint);

        private async Task SearchForServersAsync(CancellationToken cancellationToken)
        {
            UdpClient udpClient = null;
            try
            {
                LogInformation("Started searching for servers.");
                IsSearching = true;
                IPEndPoint broadcastEndPoint = new(IPAddress.Broadcast, _Port);
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    udpClient ??= new UdpClient(0)
                    {
                        EnableBroadcast = true,
                        MulticastLoopback = false
                    };
                    
                    // Send discovery packet
                    await udpClient.SendAsync(_secretBytes, _secretBytes.Length, broadcastEndPoint);

                    // Wait for responses within the timeout
                    var receiveTask = udpClient.ReceiveAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(SearchTimeout), cancellationToken);

                    var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                    if (completedTask == receiveTask)
                    {
                        var result = receiveTask.Result;
                        try
                        {
                            var response = DeserializeResponse(result.Buffer);
                            UpdateServerList(result.RemoteEndPoint, response);
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Invalid JSON response from {result.RemoteEndPoint}: {ex.Message}");
                        }
                    }
                    else
                    {
                        udpClient.Close();
                        udpClient = null;
                    }
                    RemoveExpiredServers();
                }

                LogInformation("Stopped searching for servers.");
            }
            catch (SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    LogError($"Unable to search for servers. Port {_Port} is already in use.");
                else
                    Debug.LogException(socketException, this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                IsSearching = false;
                udpClient?.Close();
            }
        }

        private void UpdateServerList(IPEndPoint endpoint, TResponse data)
        {
            var changed = false;
            lock (_serverList)
            {
                if (!_serverList.ContainsKey(endpoint) || !EqualityComparer<TResponse>.Default.Equals(_serverList[endpoint].response, data))
                    changed = true;

                _serverList[endpoint] = (data, Time.realtimeSinceStartup);
            }

            if (changed)
                _mainThreadSynchronizationContext.Post(_ => ServerListUpdated?.Invoke(), null);
        }

        private void RemoveExpiredServers()
        {
            bool changed = false;
            var now = Time.realtimeSinceStartup;
            lock (_serverList)
            {
                var toRemove = new List<IPEndPoint>();
                foreach (var kvp in _serverList)
                {
                    if ((now - kvp.Value.lastSeen) > _ServerTimeout)
                        toRemove.Add(kvp.Key);
                }

                foreach (var ep in toRemove)
                {
                    _serverList.Remove(ep);
                    changed = true;
                }
            }

            if (changed)
                _mainThreadSynchronizationContext.Post(_ => ServerListUpdated?.Invoke(), null);
        }

        private void LogInformation(string message)
        {
            if (NetworkManagerExtensions.CanLog(LoggingType.Common))
                Debug.Log($"[{nameof(ANetworkDiscovery<TResponse>)}] {message}", this);
        }

        private void LogWarning(string message)
        {
            if (NetworkManagerExtensions.CanLog(LoggingType.Warning))
                Debug.LogWarning($"[{nameof(ANetworkDiscovery<TResponse>)}] {message}", this);
        }

        private void LogError(string message)
        {
            if (NetworkManagerExtensions.CanLog(LoggingType.Error))
                Debug.LogError($"[{nameof(ANetworkDiscovery<TResponse>)}] {message}", this);
        }
    }
}
