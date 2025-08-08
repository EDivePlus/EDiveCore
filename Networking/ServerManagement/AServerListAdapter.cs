// Author: František Holubec
// Created: 15.07.2025

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.External.Signals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public abstract class AServerListAdapter : MonoBehaviour
    {
        public Dictionary<long, ServerRecord> Servers { get; } = new();
        public Signal ServerListUpdated { get; } = new();
        
        [ShowInInspector]
        private IEnumerable<ServerRecord> ServersPreview => Servers.Values;
        
        public abstract UniTask Initialize(ServerConfig serverConfig);
        public abstract void StartSearch();
        public abstract void StopSearch();

        protected void AddServer(ServerRecord serverRecord)
        {
            Servers[serverRecord.ServerID] = serverRecord;
            ServerListUpdated.Dispatch();
        }
        
        protected void AddServers(IEnumerable<ServerRecord> serverRecords)
        {
            foreach (var serverRecord in serverRecords)
            {
                Servers[serverRecord.ServerID] = serverRecord;
            }
            ServerListUpdated.Dispatch();
        }
    }
}
