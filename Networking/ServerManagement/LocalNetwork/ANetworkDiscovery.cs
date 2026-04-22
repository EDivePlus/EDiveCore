using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.LocalNetwork
{
	/// <summary>
	/// Allows clients to find servers on the local network.
	/// </summary>
	public abstract class ANetworkDiscovery<TResponse> : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("Secret to use when advertising or searching for servers.")]
		private string _Secret;
		
		[SerializeField]
		[Tooltip("Port to use when advertising or searching for servers.")]
		private ushort _Port;
		
		[SerializeField]
		[Tooltip("How long (in seconds) to wait for a response when advertising or searching for servers.")]
		private float _SearchTimeout = 2f;

		[SerializeField] 
		[Tooltip("How long in seconds before a server is removed if no response is received.")]
		private float _ServerTimeout = 10f;
		
		[SerializeField]
		private bool _Automatic;

		private readonly Dictionary<IPEndPoint, (TResponse response, float lastSeen)> _serverList = new();
        
		public IEnumerable<(IPEndPoint endPoint, TResponse response)> ServerList => _serverList.Select(s => (s.Key, s.Value.response)).ToList();
        
		public event Action ServerListUpdated;
		
		public bool IsAdvertising { get; private set; }
		public bool IsSearching { get; private set; }
		private float SearchTimeout => _SearchTimeout < 1.0f ? 1.0f : _SearchTimeout;
		
		private NetworkManager _networkManager;
		
		private SynchronizationContext _mainThreadSynchronizationContext;
		private CancellationTokenSource _cancellationTokenSource;
		private byte[] _secretBytes;
		
		private const int LINUX_SO_REUSEPORT = 15;
		private const int MACOS_SO_REUSEPORT = 0x0200;

		private void Awake()
		{
			_networkManager = GetComponentInParent<NetworkManager>();
			if (_networkManager == null)
				_networkManager = InstanceFinder.NetworkManager;

			if (_networkManager == null)
			{
				Debug.LogError($"No NetworkManager found for {gameObject.name}.");
				enabled = false;
				return;
			}
			
			_secretBytes = Encoding.UTF8.GetBytes(_Secret);
			_mainThreadSynchronizationContext = SynchronizationContext.Current;
		}

		private void OnEnable()
		{
			if (!_Automatic) 
				return;

			_networkManager.ServerManager.OnServerConnectionState += ServerConnectionStateChangedEventHandler;
			_networkManager.ClientManager.OnClientConnectionState += ClientConnectionStateChangedEventHandler;
			
			if (_networkManager.IsServerStarted) 
				AdvertiseServer();

			if (_networkManager.IsOffline) 
				SearchForServers();
		}

		private void OnDisable()
		{
			Shutdown();
		}

		private void OnDestroy()
		{
			Shutdown();
		}

		private void OnApplicationQuit()
		{
			Shutdown();
		}

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
			var transport = _networkManager.TransportManager.GetTransport(args.TransportIndex);
			if (transport is not Tugboat)
				return;
			
			if (args.ConnectionState == LocalConnectionState.Started)
			{
				AdvertiseServer();
			}
			else if (args.ConnectionState == LocalConnectionState.Stopped)
			{
				StopSearchingOrAdvertising();
			}
		}

		private void ClientConnectionStateChangedEventHandler(ClientConnectionStateArgs args)
		{
			if (_networkManager.IsServerStarted) return;

			if (args.ConnectionState == LocalConnectionState.Started)
			{
				StopSearchingOrAdvertising();
			}
			else if (args.ConnectionState == LocalConnectionState.Stopped)
			{
				SearchForServers();
			}
		}
		
		public void AdvertiseServer()
		{
			if (IsAdvertising)
			{
				Debug.LogWarning("Server is already being advertised.");
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
				Debug.LogWarning("Already searching for servers.");
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
		
		protected abstract TResponse ProcessRequest(IPEndPoint endpoint);
		
		private const int WSAECONNRESET = 10054;
		
		private void HandleReceiveException(Exception exception)
		{
			var inner = exception is AggregateException agg ? agg.Flatten().InnerException : exception;
			if (inner is SocketException { ErrorCode: WSAECONNRESET })
			{
				LogInformation("Received ICMP port-unreachable. Recycling socket.");
				return;
			}

			Debug.LogException(exception, this);
		}
		
		private static void ObserveAndForget(Task task)
		{
			if (task.IsCompleted)
			{
				_ = task.Exception;
				return;
			}
			task.ContinueWith(static t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
		}
		
		private static UdpClient CreateSharedListenClient(int port)
		{
			var client = new UdpClient();
			client.ExclusiveAddressUse = false;
			client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			var reusePort = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? LINUX_SO_REUSEPORT
				: RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? MACOS_SO_REUSEPORT
				: 0;

			if (reusePort != 0)
			{
				try
				{
					client.Client.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)reusePort, 1);
				}
				catch (SocketException)
				{
					Debug.LogWarning("SO_REUSEPORT not supported on this platform. Multiple instances of the server may not work correctly.");
				}
			}

			client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
			return client;
		}
		
		private async Task AdvertiseServerAsync(CancellationToken cancellationToken)
		{
			UdpClient listenClient = null;
			UdpClient sendClient = null;

			try
			{
				LogInformation("Started advertising server.");
				IsAdvertising = true;

				// Separate send socket on an ephemeral port so each server on the same PC
				// replies from a unique source endpoint — the client's server list is keyed
				// by IPEndPoint, so a shared source port would collapse them into one entry.
				sendClient = new UdpClient(0);

				while (!cancellationToken.IsCancellationRequested)
				{
					listenClient ??= CreateSharedListenClient(_Port);

					LogInformation("Waiting for request...");
					var receiveTask = listenClient.ReceiveAsync();
					var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
					var completedTask = await Task.WhenAny(receiveTask, cancellationTask);

					if (completedTask != receiveTask)
					{
						ObserveAndForget(receiveTask);
						break;
					}

					try
					{
						if (receiveTask.IsFaulted)
						{
							HandleReceiveException(receiveTask.Exception);
							listenClient.Close();
							listenClient = null;
							continue;
						}

						var result = receiveTask.Result;
						var receivedSecret = Encoding.UTF8.GetString(result.Buffer);
						if (receivedSecret != _Secret)
						{
							Debug.LogWarning($"Received invalid request from {result.RemoteEndPoint}.");
							continue;
						}

						LogInformation($"Received request from {result.RemoteEndPoint}.");
						var response = ProcessRequest(result.RemoteEndPoint);
						var bytes = SerializeResponse(response);
						await sendClient.SendAsync(bytes, bytes.Length, result.RemoteEndPoint);
					}
					catch (Exception exception)
					{
						Debug.LogException(exception, this);
						listenClient?.Close();
						listenClient = null;
					}
				}

				LogInformation("Stopped advertising server.");
			}
			catch (Exception exception) when (exception is not TaskCanceledException)
			{
				Debug.LogException(exception, this);
			}
			finally
			{
				IsAdvertising = false;
				LogInformation("Closing UDP clients...");
				listenClient?.Close();
				sendClient?.Close();
			}
		}
		
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
					if (udpClient == null)
					{
						udpClient = new UdpClient();
						udpClient.EnableBroadcast = true;
					}
					try
					{
						LogInformation("Sending request...");
						await udpClient.SendAsync(_secretBytes, _secretBytes.Length, broadcastEndPoint);
						
						LogInformation("Waiting for response...");
						var receiveTask = udpClient.ReceiveAsync();
						var timeoutTask = Task.Delay(TimeSpan.FromSeconds(SearchTimeout), cancellationToken);
						var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

						if (completedTask == receiveTask)
						{
							if (receiveTask.IsFaulted)
							{
								HandleReceiveException(receiveTask.Exception);
								udpClient.Close();
								udpClient = null;
							}
							else
							{
								var result = receiveTask.Result;
						
								try
								{
									LogInformation($"Received response from {result.RemoteEndPoint}.");
									var response = DeserializeResponse(result.Buffer);
									_mainThreadSynchronizationContext?.Post(_ => UpdateServerList(result.RemoteEndPoint, response), null);
								}
								catch (Exception ex)
								{
									Debug.LogWarning($"Invalid JSON response from {result.RemoteEndPoint}: {ex.Message}");
								}
							}
						}
						else
						{
							LogInformation("Timed out. Retrying...");
							ObserveAndForget(receiveTask);
							udpClient.Close();
							udpClient = null;
						}
						_mainThreadSynchronizationContext?.Post(_ => RemoveExpiredServers(), null);
					}
					catch (Exception exception)
					{
						Debug.LogException(exception, this);
						udpClient?.Close();
						udpClient = null;
					}
				}
				LogInformation("Stopped searching for servers.");
			}
			catch (Exception exception) when (exception is not TaskCanceledException)
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
			var changed = !_serverList.ContainsKey(endpoint) || !EqualityComparer<TResponse>.Default.Equals(_serverList[endpoint].response, data);
			_serverList[endpoint] = (data, Time.realtimeSinceStartup);
			if (changed)
				ServerListUpdated?.Invoke();
		}
		
		private void RemoveExpiredServers()
		{
			var changed = false;
			var now = Time.realtimeSinceStartup;

			foreach (var (endpoint, (_, lastSeen)) in _serverList.ToList())
			{
				if ((now - lastSeen) > _ServerTimeout)
				{
					_serverList.Remove(endpoint);
					changed = true;
				}
			}
			
			if (changed)
				ServerListUpdated?.Invoke();
		}

		[System.Diagnostics.Conditional("NET_DISCOVERY_LOGS")]
		private void LogInformation(string message)
		{
			if (NetworkManagerExtensions.CanLog(LoggingType.Common)) Debug.Log($"[{GetType().Name}] {message}", this);
		}
	}
}