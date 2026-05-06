// Author: František Holubec
// Created: 21.11.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
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
        [CustomValueDrawer("CustomEndpointDrawer")]
        public List<AServerEndpoint> Endpoints = new();

        public async UniTask<bool> PrepareForConnect()
        {
            if (Endpoints == null)
                return false;

            foreach (var endpoint in Endpoints)
            {
                if (endpoint == null)
                    continue;
                if (await endpoint.PrepareForConnect())
                    return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private AServerEndpoint CustomEndpointDrawer(AServerEndpoint value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            callNextDrawer(label);
            if (GUILayout.Button("Connect"))
            {
                if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
                {
                    serverManager.ConnectToServer(this, value);
                }
            }
            return value;
        }
#endif
        
    }
}
