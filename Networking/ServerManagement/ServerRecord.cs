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
        public string InstanceID;
        public string ServerName;
        public int MaxPlayers;
        public int CurrentPlayers;
        public DateTime LastUpdated;

        public ServerRecord() { }
        public ServerRecord(string instanceID)
        {
            InstanceID = instanceID;
        }

        [HideReferenceObjectPicker]
        [ListDrawerSettings(OnEndListElementGUI = "DrawEndpointConnect")]
        public List<AServerEndpoint> Endpoints = new();

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
                Endpoints = new List<AServerEndpoint> { endpoint }
            };
            return record;
        }
    }
}
