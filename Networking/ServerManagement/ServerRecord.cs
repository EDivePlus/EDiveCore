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
        public long ServerID;
        public string ServerName;
        public int MaxPlayers;
        public int CurrentPlayers;
        public DateTime LastUpdated;

        public ServerRecord() { }
        public ServerRecord(long serverID)
        {
            ServerID = serverID;
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
    }
}
