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
        public Dictionary<long, AServerRecord> Servers { get; } = new();
        public Signal ServerListUpdated { get; } = new();

        protected ServerConfig _serverConfig;
        
        [ShowInInspector]
        private IEnumerable<AServerRecord> ServersPreview => Servers.Values;

        public async UniTask Initialize(ServerConfig serverConfig)
        {
            _serverConfig = serverConfig;
            await Initialize();
        }
        
        public virtual UniTask Initialize() => UniTask.CompletedTask;

        public virtual void StartServer(){}
        public virtual void StopServer(){}
        
        public virtual void StartSearch(){}
        public virtual void StopSearch(){}

        protected void AddServer(AServerRecord serverRecord)
        {
            Servers[serverRecord.ServerID] = serverRecord;
            ServerListUpdated.Dispatch();
        }
        
        protected void AddServers(IEnumerable<AServerRecord> serverRecords)
        {
            foreach (var serverRecord in serverRecords)
            {
                Servers[serverRecord.ServerID] = serverRecord;
            }
            ServerListUpdated.Dispatch();
        }
    }
}
