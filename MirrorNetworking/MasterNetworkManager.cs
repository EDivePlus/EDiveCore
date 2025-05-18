// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.Core.Services;
using EDIVE.External.Signals;
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

        public Signal<TransportError, string> ClientError { get; } = new();

        public int ConnectionCount => NetworkServer.connections.Count;

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
        }

        public override void OnStopServer()
        {
            ServerStopped.Dispatch();
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
            Debug.Log($"Client disconnecting {conn?.connectionId}");
            ServerClientDisconnecting.Dispatch(conn);
            base.OnServerDisconnect(conn);
            Debug.Log($"Client disconnected {conn?.connectionId}");
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
            Debug.Log($"Connecting to: {networkAddress}");
            ClientStarted.Dispatch();
        }

        public override void OnStopClient()
        {
            Debug.Log("Client stopped");
            ClientStopped.Dispatch();
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("Connected to server");
            ClientConnected.Dispatch();
            // TODO wait for server response somewhere and call OnConnected client event
        }

        public override void OnClientError(TransportError error, string reason)
        {
            Debug.LogError("Client error: " + reason);
            ClientError.Dispatch(error, reason);
        }

        public override void OnClientDisconnect()
        {
            Debug.Log("Disconnected from server");
            ClientDisconnected.Dispatch();
        }
    }
}
