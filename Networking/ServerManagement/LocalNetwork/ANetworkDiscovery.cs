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
	/// <summary>
	/// Allows clients to find servers on the local network.
	/// </summary>
	public abstract class ANetworkDiscovery<TResponse> : MonoBehaviour
	{
		/// <summary>
		/// The <see cref="FishNet.Managing.NetworkManager"/> to use.
		/// </summary>
		private NetworkManager _networkManager;
		
		/// <summary>
		/// The secret to use when advertising or searching for servers.
		/// </summary>
		[SerializeField]
		[Tooltip("Secret to use when advertising or searching for servers.")]
		private string _Secret;
		
		/// <summary>
		/// Port to use when advertising or searching for servers.
		/// </summary>
		[SerializeField]
		[Tooltip("Port to use when advertising or searching for servers.")]
		private ushort _Port;

		/// <summary>
		/// How long (in seconds) to wait for a response when advertising or searching for servers.
		/// </summary>
		[SerializeField]
		[Tooltip("How long (in seconds) to wait for a response when advertising or searching for servers.")]
		private float _SearchTimeout = 2f;

		[SerializeField] 
		[Tooltip("How long in seconds before a server is removed if no response is received.")]
		private float _ServerTimeout = 10f;
		
		/// <summary>
		/// If true, will automatically start advertising or searching for servers when the NetworkManager starts or stops.
		/// </summary>
		[SerializeField]
		private bool _Automatic;

		private readonly Dictionary<IPEndPoint, (TResponse response, float lastSeen)> _serverList = new();
        
		public IEnumerable<(IPEndPoint endPoint, TResponse response)> ServerList => _serverList.Select(s => (s.Key, s.Value.response)).ToList();
        
		public event Action ServerListUpdated;
		
		/// <summary>
		/// True if the server is being advertised.
		/// </summary>
		public bool IsAdvertising { get; private set; }

		/// <summary>
		/// True if the client is searching for servers.
		/// </summary>
		public bool IsSearching { get; private set; }

		/// <summary>
		/// How long (in seconds) to wait for a response when advertising or searching for servers.
		/// </summary>
		private float SearchTimeout => _SearchTimeout < 1.0f ? 1.0f : _SearchTimeout;
		
		/// <summary>
		/// The synchronizationContext of the main thread.
		/// </summary>
		private SynchronizationContext _mainThreadSynchronizationContext;

		/// <summary>
		/// Used to cancel the search or advertising.
		/// </summary>
		private CancellationTokenSource _cancellationTokenSource;

		/// <summary>
		/// A byte-representation of the secret to use when advertising or searching for servers.
		/// </summary>
		private byte[] _secretBytes;

		private void Awake()
		{
			_networkManager = GetComponentInParent<NetworkManager>();
			if (_networkManager == null)
				_networkManager = InstanceFinder.NetworkManager;

			if (_networkManager == null)
			{
				LogError($"No NetworkManager found for {gameObject.name}.");
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

		/// <summary>
		/// Shuts the NetworkDiscovery.
		/// </summary>
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

		/// <summary>
		/// Advertises the server on the local network.
		/// </summary>
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

		/// <summary>
		/// Searches for servers on the local network.
		/// </summary>
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

		/// <summary>
		/// Stops searching or advertising.
		/// </summary>
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

		
		/// <summary>
		/// Advertises the server on the local network.
		/// </summary>
		/// <param name="cancellationToken">Used to cancel advertising.</param>
		private async Task AdvertiseServerAsync(CancellationToken cancellationToken)
		{
			UdpClient udpClient = null;

			try
			{
				LogInformation("Started advertising server.");
				IsAdvertising = true;

				while (!cancellationToken.IsCancellationRequested)
				{
					udpClient ??= new UdpClient(_Port);

					LogInformation("Waiting for request...");
					var timeoutTask = Task.Delay(TimeSpan.FromSeconds(SearchTimeout), cancellationToken);
					try
					{
						var receiveTask = udpClient.ReceiveAsync();
						var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

						if (completedTask == receiveTask)
						{
							var result = receiveTask.Result;
							var receivedSecret = Encoding.UTF8.GetString(result.Buffer);
							if (receivedSecret == _Secret)
							{
								LogInformation($"Received request from {result.RemoteEndPoint}.");
							
								var response = ProcessRequest(result.RemoteEndPoint);
								var bytes = SerializeResponse(response);
								await udpClient.SendAsync(bytes, bytes.Length, result.RemoteEndPoint);
							}
							else
							{
								LogWarning($"Received invalid request from {result.RemoteEndPoint}.");
							}
						}
						else
						{
							LogInformation("Timed out. Retrying...");
							udpClient.Close();
							udpClient = null;
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception, this);
						udpClient.Close();
						udpClient = null;
					}
					
					// wait for timeout
					await timeoutTask;
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
				LogInformation("Closing UDP client...");
				udpClient?.Close();
			}
		}

		/// <summary>
		/// Searches for servers on the local network.
		/// </summary>
		/// <param name="cancellationToken">Used to cancel searching.</param>
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
					udpClient ??= new UdpClient();
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
							var result = receiveTask.Result;
						
							try
							{
								LogInformation($"Received response from {result.RemoteEndPoint}.");
								var response = DeserializeResponse(result.Buffer);
								_mainThreadSynchronizationContext.Post(_ => UpdateServerList(result.RemoteEndPoint, response), null);
							}
							catch (Exception ex)
							{
								LogWarning($"Invalid JSON response from {result.RemoteEndPoint}: {ex.Message}");
							}
						}
						else
						{
							LogInformation("Timed out. Retrying...");
							udpClient.Close();
							udpClient = null;
						}
						_mainThreadSynchronizationContext.Post(_ => RemoveExpiredServers(), null);
					}
					catch (Exception exception)
					{
						Debug.LogException(exception, this);
						udpClient.Close();
						udpClient = null;
					}
				}
				LogInformation("Stopped searching for servers.");
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
		

		/// <summary>
		/// Logs a message if the NetworkManager can log.
		/// </summary>
		/// <param name="message">Message to log.</param>
		[System.Diagnostics.Conditional("NET_DISCOVERY_LOGS")]
		private void LogInformation(string message)
		{
			if (NetworkManagerExtensions.CanLog(LoggingType.Common)) Debug.Log($"[{GetType().Name}] {message}", this);
		}

		/// <summary>
		/// Logs a warning if the NetworkManager can log.
		/// </summary>
		/// <param name="message">Message to log.</param>
		private void LogWarning(string message)
		{
			if (NetworkManagerExtensions.CanLog(LoggingType.Warning)) Debug.LogWarning($"[{GetType().Name}] {message}", this);
		}

		/// <summary>
		/// Logs an error if the NetworkManager can log.
		/// </summary>
		/// <param name="message">Message to log.</param>
		private void LogError(string message)
		{
			if (NetworkManagerExtensions.CanLog(LoggingType.Error)) Debug.LogError($"[{GetType().Name}] {message}", this);
		}
	}
}