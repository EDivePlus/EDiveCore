// Author: František Holubec
// Created: 21.11.2025

using System;
using System.Collections.Generic;
using EDIVE.Core;
using EDIVE.Core.Versions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public class ServerRecord : IEquatable<ServerRecord>
    {
        [HideReferenceObjectPicker]
        [ListDrawerSettings(OnEndListElementGUI = "DrawEndpointConnect")]
        public List<AServerEndpoint> Endpoints = new();
        
        public event Action<ServerRecord> StateChanged;
        
        private string _instanceID;
        private string _serverName;
        private int _maxPlayers;
        private int _currentPlayers;
        private DateTime _lastUpdated;
        private string _joinCode;
        private AppVersion _version;
        
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

        public AppVersion Version
        {
            get => _version;
            set
            {
                if (_version == value) 
                    return;
                _version = value;
                StateChanged?.Invoke(this);
            }
        }

        public ServerRecord() { }
        public ServerRecord(string instanceID)
        {
            InstanceID = instanceID;
        }

        public static ServerRecord CreateUnknown(AServerEndpoint endpoint)
        {
            var record = new ServerRecord(Guid.NewGuid().ToString())
            {
                ServerName = "Unknown Server",
                MaxPlayers = 0,
                CurrentPlayers = 0,
                LastUpdated = DateTime.Now,
                Endpoints = new List<AServerEndpoint> { endpoint },
                JoinCode = string.Empty,
                Version = AppVersion.ZERO
            };
            return record;
        }
        
        private static bool EndpointsEqual(List<AServerEndpoint> a, List<AServerEndpoint> b)
        {
            if (ReferenceEquals(a, b)) return true;

            var countA = a?.Count ?? 0;
            var countB = b?.Count ?? 0;
            if (countA != countB) return false;

            for (var i = 0; i < countA; i++)
            {
                var ea = a[i];
                var eb = b[i];
                if (ea == null || eb == null)
                {
                    if (!ReferenceEquals(ea, eb)) return false;
                    continue;
                }
                if (!ea.Equals(eb)) return false;
            }
            return true;
        }
        
        public bool Equals(ServerRecord other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            return InstanceID == other.InstanceID
                && ServerName == other.ServerName
                && MaxPlayers == other.MaxPlayers
                && CurrentPlayers == other.CurrentPlayers
                && JoinCode == other.JoinCode
                && Version == other.Version
                && EndpointsEqual(Endpoints, other.Endpoints);
        }

        public override bool Equals(object obj) => Equals(obj as ServerRecord);
        
        public override int GetHashCode() => InstanceID != null ? InstanceID.GetHashCode() : 0;
        
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
    }
}
