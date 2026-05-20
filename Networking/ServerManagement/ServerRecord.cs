// Author: František Holubec
// Created: 21.11.2025

using System;
using System.Collections.Generic;
using EDIVE.Core;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public class ServerRecord
    {
        private string _instanceID;
        public string InstanceID
        {
            get => _instanceID;
            set
            {
                if (_instanceID == value) 
                    return;
                _instanceID = value;
                StateChanged?.Invoke(this);
            }
        }
        
        private string _serverName;
        public string ServerName
        {
            get => _serverName;
            set
            {
                if (_serverName == value) 
                    return;
                _serverName = value;
                StateChanged?.Invoke(this);
            }
        }
        
        private int _maxPlayers;
        public int MaxPlayers
        {
            get => _maxPlayers;
            set
            {
                if (_maxPlayers == value) 
                    return;
                _maxPlayers = value;
                StateChanged?.Invoke(this);
            }
        }
        
        private int _currentPlayers;
        public int CurrentPlayers
        {
            get => _currentPlayers;
            set
            {
                if (_currentPlayers == value) 
                    return;
                _currentPlayers = value;
                StateChanged?.Invoke(this);
            }
        }
        
        private DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                if (_lastUpdated == value) 
                    return;
                _lastUpdated = value;
                StateChanged?.Invoke(this);
            }
        }
        
        private string _joinCode;
        public string JoinCode
        {
            get => _joinCode;
            set
            {
                if (_joinCode == value) 
                    return;
                _joinCode = value;
                StateChanged?.Invoke(this);
            }
        }

        public ServerRecord() { }
        public ServerRecord(string instanceID)
        {
            InstanceID = instanceID;
        }

        [HideReferenceObjectPicker]
        [ListDrawerSettings(OnEndListElementGUI = "DrawEndpointConnect")]
        public List<AServerEndpoint> Endpoints = new();
        
        public event Action<ServerRecord> StateChanged;

#if UNITY_EDITOR
        [Button]
        private void ConnectAny()
        {
            if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
            {
                serverManager.ConnectToServer(this);
            }
        }
        
        [UsedImplicitly]
        private void DrawEndpointConnect(int index)
        {
            var value = Endpoints[index];
            if (GUILayout.Button("Connect"))
            {
                if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
                {
                    serverManager.ConnectToServer(this, value);
                }
            }
        }
#endif
        
        public static ServerRecord CreateUnknown(AServerEndpoint endpoint)
        {
            var record = new ServerRecord(Guid.NewGuid().ToString())
            {
                ServerName = "Unknown Server",
                MaxPlayers = 0,
                CurrentPlayers = 0,
                LastUpdated = DateTime.Now,
                Endpoints = new List<AServerEndpoint> { endpoint },
                JoinCode = string.Empty
            };
            return record;
        }
    }
}
