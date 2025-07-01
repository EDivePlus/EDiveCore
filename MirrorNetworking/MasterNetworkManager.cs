// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.Core.Restart;
using EDIVE.Core.Services;
using EDIVE.External.Signals;
using EDIVE.MirrorNetworking.Utils;
using LightReflectiveMirror;
using Mirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking
{
    public class MasterNetworkManager : NetworkManager, IService, ILoadable
    {
        public Signal ServerStarted { get; } = new();
        public Signal ServerStopped { get; } = new();
        public Signal<NetworkConnectionToClient> ServerPlayerAdded { get; } = new();
        public Signal<NetworkConnectionToClient> ServerClientConnecting { get; } = new();
        public Signal<NetworkConnectionToClient> ServerClientConnected { get; } = new();
        public Signal<NetworkConnectionToClient> ServerClientDisconnecting { get; } = new();
        public Signal<NetworkConnectionToClient> ServerClientDisconnected { get; } = new();
        public Signal<NetworkConnectionToClient> ServerClientReady { get; } = new();

        public Signal ClientStarted { get; } = new();
        public Signal ClientStopped { get; } = new();
        public Signal ClientConnected { get; } = new();
        public Signal ClientDisconnected { get; } = new();
        public Signal<ClientConnectionState> ClientConnectionStateChanged { get; } = new();

        public Signal<bool, NetworkRuntimeMode> RuntimeStateChanged { get; } = new();
        public Signal<TransportError, string> ClientError { get; } = new();

        public int ConnectionCount => NetworkServer.connections.Count;
        public NetworkRuntimeMode CurrentNetworkMode => mode switch 
        {
            NetworkManagerMode.Offline or NetworkManagerMode.ClientOnly => NetworkRuntimeMode.Client,
            NetworkManagerMode.ServerOnly => NetworkRuntimeMode.Server,
            NetworkManagerMode.Host => NetworkRuntimeMode.Host,
            _ => throw new ArgumentOutOfRangeException()
        };

        public ClientConnectionState ClientConnectionState
        {
            get => _clientConnectionState;
            private set
            {
                if (_clientConnectionState == value)
                    return;
                _clientConnectionState = value;
                ClientConnectionStateChanged.Dispatch(value);
            }
        }
        private ClientConnectionState _clientConnectionState = ClientConnectionState.Disconnected;

        public string CurrentServerName
        {
            get
            {
                if (this.TryGetTransport<LightReflectiveMirrorTransport>(out var lrm))
                    return lrm.serverId;

                return networkAddress;
            }
        }

        public override void Start()
        {
            // Nothing, we will start the server or client in the load finalizer
        }

        public UniTask Load(Action<float> progressCallback)
        {
            AppCore.Services.Register(this);
            return UniTask.CompletedTask;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AppCore.Services.Unregister(this);
        }

        public override void OnStartServer()
        {
            ServerStarted.Dispatch();
            RuntimeStateChanged.Dispatch(true, mode == NetworkManagerMode.Host ? NetworkRuntimeMode.Host : NetworkRuntimeMode.Server);
        }

        public override void OnStopServer()
        {
            ServerStopped.Dispatch();
            RuntimeStateChanged.Dispatch(false, mode == NetworkManagerMode.Host ? NetworkRuntimeMode.Host : NetworkRuntimeMode.Server);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Debug.Log("Adding player for connection with ID: " + conn.connectionId);
            base.OnServerAddPlayer(conn);
            Debug.Log("Player added.");
            ServerPlayerAdded.Dispatch(conn);
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            Debug.Log($"Client connecting {conn.connectionId}");
            ServerClientConnecting.Dispatch(conn);
            Debug.Log($"Total connections {ConnectionCount}");
            if (ConnectionCount > maxConnections)
            {
                conn.Disconnect();
                Debug.Log("Too many client connections. Could not connect new client.");
                // TODO send message to client
                return;
            }
            Debug.Log($"Client connected {conn.connectionId}");
            ServerClientConnected.Dispatch(conn);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn == null) return;
            Debug.Log($"Client disconnecting {conn.connectionId}");
            ServerClientDisconnecting.Dispatch(conn);
            base.OnServerDisconnect(conn);
            Debug.Log($"Client disconnected {conn.connectionId}");
            ServerClientDisconnected.Dispatch(conn);
        }

        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
            Debug.Log($"Client ready, ID: {conn.connectionId}");
            ServerClientReady.Dispatch(conn);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log($"Connecting to: {networkAddress}");
            ClientConnectionState = ClientConnectionState.Connecting;
            ClientStarted.Dispatch();
            if (mode == NetworkManagerMode.ClientOnly)
                RuntimeStateChanged.Dispatch(true, NetworkRuntimeMode.Client);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            Debug.Log("Client stopped");
            ClientConnectionState = ClientConnectionState.Disconnected;
            ClientStopped.Dispatch();
            if (mode == NetworkManagerMode.ClientOnly)
                RuntimeStateChanged.Dispatch(false, NetworkRuntimeMode.Client);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("Connected to server");
            ClientConnectionState = ClientConnectionState.Connected;
            ClientConnected.Dispatch();
            // TODO wait for server response somewhere and call OnConnected client event
        }

        public override void OnClientError(TransportError error, string reason)
        {
            base.OnClientError(error, reason);
            Debug.LogError("Client error: " + reason);
            ClientError.Dispatch(error, reason);
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Debug.Log("Disconnected from server");
            ClientConnectionState = ClientConnectionState.Disconnected;
            ClientDisconnected.Dispatch();
        }

        public void StartRuntime(NetworkRuntimeMode runtimeMode)
        {
            ClientConnectionState = ClientConnectionState.Connecting;
            switch (runtimeMode)
            {
                case NetworkRuntimeMode.Client:
                    StartClient();
                    break;
                case NetworkRuntimeMode.Server:
                    StartServer();
                    break;
                case NetworkRuntimeMode.Host:
                    StartHost();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtimeMode), runtimeMode, null);
            }
        }

        public void StopRuntime()
        {
            ClientConnectionState = ClientConnectionState.Disconnected;
            switch (mode)
            {
                case NetworkManagerMode.Offline:
                    break;
                case NetworkManagerMode.ServerOnly:
                    StopServer();
                    break;
                case NetworkManagerMode.ClientOnly:
                    StopClient();
                    break;
                case NetworkManagerMode.Host:
                    StopHost();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        [ExecuteOnAppRestart(-90)]
        public static UniTask OnAppRestart()
        {
            if (!AppCore.Services.TryGet<MasterNetworkManager>(out var networkManager))
            {
                Debug.LogError("Cannot stop runtime, missing network manager");
                return UniTask.CompletedTask;
            }
            
            networkManager.StopRuntime();
            return UniTask.CompletedTask;
        }
    }
}
